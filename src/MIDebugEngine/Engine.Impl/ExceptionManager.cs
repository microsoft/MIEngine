// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Threading;
using Microsoft.DebugEngineHost;

namespace Microsoft.MIDebugEngine
{
    internal class ExceptionManager
    {
        // ***************** DESIGN NOTES *************************
        // At least at the time of the creation (VS 2015 RTM), the exception settings dialog pushes
        // multiple redundant updates to the settings when exception change. So if we directly wire
        // up receiving updates via the AD7 interfaces to providing them to the backend debugger
        // then we would make many more calls that would otherwise be necessary. To avoid this problem
        // we instead queue the update when the AD7 call comes in, and flush the queue on a delay.
        // To avoid the possiblity of leaving break state before the queue is flushed, we call
        // back into the exception manager (EnsureSettingsUpdated) to wait for the settings to be flushed.
        //

        private readonly MICommandFactory _commandFactory;
        private readonly WorkerThread _worker;
        private readonly ReadOnlyDictionary<Guid, ExceptionCategorySettings> _categoryMap;
        private readonly ISampleEngineCallback _callback;
        private bool _initialSettingssSent;

        private readonly object _updateLock = new object();
        private int? _lastUpdateTime;
        private Task _updateTask;
        private CancellationTokenSource _updateDelayCancelSource;

    private class SettingsUpdates
        {
            // Threading note: these are only modified on the main thread
            public ExceptionBreakpointStates? NewCategoryState;
            public readonly Dictionary<string, ExceptionBreakpointStates> RulesToAdd;
            public readonly HashSet<string> RulesToRemove = new HashSet<string>();

            public SettingsUpdates(/*OPTIONAL*/ ExceptionBreakpointStates? initialNewCategoryState, /*OPTIONAL*/ ReadOnlyDictionary<string, ExceptionBreakpointStates> initialRuleChanges)
            {
                this.NewCategoryState = initialNewCategoryState;

                // The dictionary constructor which takes a read only dictionary is unhappy if we pass in null, so switch off which constructor we call
                if (initialRuleChanges != null)
                    this.RulesToAdd = new Dictionary<string, ExceptionBreakpointStates>(initialRuleChanges);
                else
                    this.RulesToAdd = new Dictionary<string, ExceptionBreakpointStates>();
            }
        }

        /// <summary>
        /// Holder class used to hold a settings update + a lock on it, when
        /// the holder is disposed, we will drop the lock and ensure that we have
        /// queued the processing of updates.
        /// </summary>
        private class SettingsUpdateHolder : IDisposable
        {
            /// <summary>
            /// [Required] The SettingsUpdates being held by the holder
            /// </summary>
            public readonly SettingsUpdates Value;
            private readonly ExceptionManager _parent;
            private readonly object _updateLock;

            public SettingsUpdateHolder(SettingsUpdates value, ExceptionManager parent, object updateLock)
            {
                this.Value = value;
                _parent = parent;
                _updateLock = updateLock;
                Monitor.Enter(_updateLock);
            }

            public void Dispose()
            {
                Monitor.Exit(_updateLock);
                _parent.EnsureUpdateTaskStarted();
            }
        }

        private class ExceptionCategorySettings
        {
            private readonly ExceptionManager _parent;
            public readonly string CategoryName;
            public readonly ExceptionBreakpointStates DefaultCategoryState;
            public readonly ReadOnlyDictionary<string, ExceptionBreakpointStates> DefaultRules;

            // Threading note: these are only read or updated by the FlushSettingsUpdates thread (in UpdateCategory), and we
            // guarantee that there will only be one active FlushSettingsUpdates task at a time
            public ExceptionBreakpointStates CategoryState;
            public readonly Dictionary<string, ulong> CurrentRules = new Dictionary<string, ulong>();

            private readonly object _updateLock = new object();
            /*OPTIONAL*/
            private SettingsUpdates _settingsUpdate;

            public ExceptionCategorySettings(ExceptionManager parent, HostConfigurationSection categoryKey, string categoryName)
            {
                _parent = parent;
                this.CategoryName = categoryName;
                this.DefaultCategoryState = RegistryToExceptionBreakpointState(categoryKey.GetValue("*"));
                Dictionary<string, ExceptionBreakpointStates> exceptionSettings = new Dictionary<string, ExceptionBreakpointStates>();
                foreach (string valueName in categoryKey.GetValueNames())
                {
                    if (string.IsNullOrEmpty(valueName) || valueName == "*" || !ExceptionManager.IsSupportedException(valueName))
                        continue;

                    ExceptionBreakpointStates value = RegistryToExceptionBreakpointState(categoryKey.GetValue(valueName));
                    if (value == this.DefaultCategoryState)
                    {
                        Debug.Fail("Redundant exception trigger found in the registry.");
                        continue;
                    }

                    exceptionSettings.Add(valueName, value);
                }
                this.DefaultRules = new ReadOnlyDictionary<string, ExceptionBreakpointStates>(exceptionSettings);
                _settingsUpdate = new SettingsUpdates(this.DefaultCategoryState, this.DefaultRules);
            }

            public SettingsUpdateHolder GetSettingsUpdate()
            {
                lock (_updateLock)
                {
                    if (_settingsUpdate == null)
                    {
                        _settingsUpdate = new SettingsUpdates(null, null);
                    }

                    return new SettingsUpdateHolder(_settingsUpdate, _parent, _updateLock);
                }
            }

            public SettingsUpdates DetachSettingsUpdate()
            {
                lock (_updateLock)
                {
                    SettingsUpdates returnValue = _settingsUpdate;
                    _settingsUpdate = null;
                    return returnValue;
                }
            }

            private static ExceptionBreakpointStates RegistryToExceptionBreakpointState(/*OPTIONAL*/ object registryValue)
            {
                if (registryValue == null || !(registryValue is int))
                    return ExceptionBreakpointStates.None;

                enum_EXCEPTION_STATE value = (enum_EXCEPTION_STATE)(int)registryValue;
                return ExceptionManager.ToExceptionBreakpointState(value);
            }
        };

        public ExceptionManager(MICommandFactory commandFactory, WorkerThread worker, ISampleEngineCallback callback, /*OPTIONAL*/ HostConfigurationStore configStore)
        {
            Debug.Assert(commandFactory != null, "Missing commandFactory");
            Debug.Assert(worker != null, "Missing worker");
            Debug.Assert(callback != null, "Missing callback");

            _commandFactory = commandFactory;
            _worker = worker;
            _callback = callback;
            _categoryMap = ReadDefaultSettings(configStore);
        }

        public void RemoveAllSetExceptions(Guid guidType)
        {
            if (guidType == Guid.Empty)
            {
                foreach (var key in _categoryMap.Keys)
                {
                    RemoveAllSetExceptions(key);
                }
            }
            else
            {
                ExceptionCategorySettings categorySettings;
                if (!_categoryMap.TryGetValue(guidType, out categorySettings))
                {
                    return; // not a category that we care about
                }

                using (var settingsUpdateHolder = categorySettings.GetSettingsUpdate())
                {
                    settingsUpdateHolder.Value.RulesToAdd.Clear();
                    settingsUpdateHolder.Value.RulesToRemove.Clear();

                    settingsUpdateHolder.Value.NewCategoryState = categorySettings.DefaultCategoryState;
                    foreach (var defaultRule in categorySettings.DefaultRules)
                    {
                        settingsUpdateHolder.Value.RulesToAdd.Add(defaultRule.Key, defaultRule.Value);
                    }
                }
            }
        }

        public void RemoveSetException(ref EXCEPTION_INFO exceptionInfo)
        {
            ExceptionCategorySettings categorySettings;
            if (!_categoryMap.TryGetValue(exceptionInfo.guidType, out categorySettings))
            {
                return; // not a category that we care about
            }

            if (categorySettings.CategoryName.Equals(exceptionInfo.bstrExceptionName, StringComparison.OrdinalIgnoreCase))
            {
                // We treat removing an exception category to be the same as setting all the exceptions in the category to break unhandled.
                EXCEPTION_INFO setExceptionInfo = exceptionInfo;
                setExceptionInfo.dwState = enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE;
                SetException(ref setExceptionInfo);
            }
            else
            {
                string exceptionName = GetExceptionId(exceptionInfo.bstrExceptionName, exceptionInfo.dwCode);

                if (!IsSupportedException(exceptionName))
                {
                    return;
                }

                using (var settingsUpdateHolder = categorySettings.GetSettingsUpdate())
                {
                    settingsUpdateHolder.Value.RulesToAdd.Remove(exceptionName);
                    settingsUpdateHolder.Value.RulesToRemove.Add(exceptionName);
                }
            }
        }

        public void SetException(ref EXCEPTION_INFO exceptionInfo)
        {
            ExceptionCategorySettings categorySettings;
            if (!_categoryMap.TryGetValue(exceptionInfo.guidType, out categorySettings))
            {
                return; // not a category that we care about
            }

            var newState = ToExceptionBreakpointState(exceptionInfo.dwState);

            if (categorySettings.CategoryName.Equals(exceptionInfo.bstrExceptionName, StringComparison.OrdinalIgnoreCase))
            {
                // Setting the exception category will clear all the existing rules in that category

                SetCategory(categorySettings, newState);
            }
            else
            {
                string exceptionName = GetExceptionId(exceptionInfo.bstrExceptionName, exceptionInfo.dwCode);

                if (!IsSupportedException(exceptionName))
                {
                    return;
                }

                using (var settingsUpdateHolder = categorySettings.GetSettingsUpdate())
                {
                    settingsUpdateHolder.Value.RulesToRemove.Remove(exceptionName);
                    settingsUpdateHolder.Value.RulesToAdd[exceptionName] = newState;
                }
            }
        }

        public void SetAllExceptions(enum_EXCEPTION_STATE dwState)
        {
            var newState = ToExceptionBreakpointState(dwState);

            foreach (var pair in _categoryMap)
            {
                SetCategory(pair.Value, newState);
            }
        }

        public int TryGetExceptionBreakpoint(string bkptno, out EXCEPTION_INFO exceptionInfo)
        {
            const string CppExceptionCategoryString = "{3A12D0B7-C26C-11D0-B442-00A0244A1DD2}";
            exceptionInfo = new EXCEPTION_INFO();
            ExceptionCategorySettings categorySettings;
            if (_categoryMap.TryGetValue(new Guid(CppExceptionCategoryString), out categorySettings))
            {
                ulong breakpointNumber = Convert.ToUInt32(bkptno, CultureInfo.InvariantCulture);
                if (categorySettings.CurrentRules.ContainsValue(breakpointNumber))
                {
                    // test -- need to delete; these are placeholder values
                    exceptionInfo.bstrExceptionName = categorySettings.CategoryName;
                    exceptionInfo.dwCode = (uint)breakpointNumber;
                    bool isBreakThrown = categorySettings.CategoryState == ExceptionBreakpointStates.BreakThrown; // this is true, but why is dwState still NONE?
                    exceptionInfo.dwState = isBreakThrown ? (enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE) : enum_EXCEPTION_STATE.EXCEPTION_NONE;
                    exceptionInfo.guidType = new Guid(CppExceptionCategoryString);
                    return Constants.S_OK;
                }
            }
            return Constants.E_FAIL;
        }

        private static void SetCategory(ExceptionCategorySettings categorySettings, ExceptionBreakpointStates newState)
        {
            using (var settingsUpdateHolder = categorySettings.GetSettingsUpdate())
            {
                settingsUpdateHolder.Value.NewCategoryState = newState;
                settingsUpdateHolder.Value.RulesToAdd.Clear();
                settingsUpdateHolder.Value.RulesToRemove.Clear();
            }
        }

        private void EnsureUpdateTaskStarted()
        {
            lock (_updateLock)
            {
                // Do nothing until the first call to EnsureSettingsUpdated
                if (_initialSettingssSent)
                {
                    if (_updateTask == null)
                    {
                        _lastUpdateTime = null;
                        _updateDelayCancelSource = new CancellationTokenSource();
                        _updateTask = Task.Run(FlushSettingsUpdates);
                    }
                    else
                    {
                        _lastUpdateTime = Environment.TickCount;
                    }
                }
            }
        }

        /// <summary>
        /// Called before resuming the target process to make sure that all updates have been sent
        /// </summary>
        /// <returns>Task which is signaled when the operation is complete</returns>
        public Task EnsureSettingsUpdated()
        {
            lock (_updateLock)
            {
                if (_updateTask != null)
                {
                    // If we are still delaying our processing, stop delaying it
                    _updateDelayCancelSource.Cancel();

                    return _updateTask;
                }
                else if (!_initialSettingssSent && _categoryMap.Count > 0)
                {
                    // The initial resume is special in how we schedule it -- we wait until the first call to
                    // EnsureSettingsUpdated to run it, and we kick it off from this thread.
                    _initialSettingssSent = true;
                    _lastUpdateTime = null;
                    // immediately cancel the delay since we don't want one
                    _updateDelayCancelSource = new CancellationTokenSource();
                    _updateDelayCancelSource.Cancel();
                    Task updateTask = FlushSettingsUpdates();
                    if (!updateTask.IsCompleted)
                    {
                        _updateTask = updateTask;
                    }
                    return updateTask;
                }
                else
                {
                    // No task is running, so just return an already signaled task
                    return Task.FromResult<object>(null);
                }
            }
        }

        private async Task FlushSettingsUpdates()
        {
            while (true)
            {
                // Delay sending updates until it has been ~50 ms since we have seen an update
                try
                {
                    while (!_updateDelayCancelSource.IsCancellationRequested)
                    {
                        await Task.Delay(50, _updateDelayCancelSource.Token);

                        lock (_updateLock)
                        {
                            if (_lastUpdateTime.HasValue)
                            {
                                uint millisecondsSinceLastUpdate = unchecked((uint)(Environment.TickCount - _lastUpdateTime.Value));

                                // Clear this so that we don't think there is an unprocessed update at the end
                                _lastUpdateTime = null;

                                // Pick a number slightly less than the ms that we pass to Task.Delay as the resolution on Environment.TickCount is not great
                                // and anyway we aren't trying to be precise as to how long we wait.
                                if (millisecondsSinceLastUpdate >= 45)
                                    break;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Calls to EnsureSettingsUpdated cancel the delay
                }

                // Now send updates
                try
                {
                    foreach (var categoryPair in _categoryMap)
                    {
                        ExceptionCategorySettings categorySettings = categoryPair.Value;

                        SettingsUpdates settingsUpdate = categorySettings.DetachSettingsUpdate();
                        if (settingsUpdate == null)
                        {
                            continue;
                        }

                        await UpdateCategory(categoryPair.Key, categorySettings, settingsUpdate);
                    }
                }
                catch (MIException e)
                {
                    _callback.OnError(string.Format(CultureInfo.CurrentCulture, ResourceStrings.ExceptionSettingsError, e.Message));
                }

                lock (_updateLock)
                {
                    if (_lastUpdateTime == null)
                    {
                        // No more updates have been posted since the start of this iteration of the loop, we are done.
                        _updateTask = null;
                        _updateDelayCancelSource = null;
                        break;
                    }
                    else
                    {
                        // An update may have been posted since our last trip arround the category loop, go again
                        _lastUpdateTime = null;
                        continue;
                    }
                }
            }
        }

        private bool IsCppExceptionsCategoryChecked(SettingsUpdates updates)
        {
            // C++ exceptions category is checked when NewCategoryState is BreakThrown and all RulesToAdd contain BreakThrown
            if (updates.NewCategoryState == ExceptionBreakpointStates.BreakThrown)
            {
                foreach (string rule in updates.RulesToAdd.Keys)
                {
                    if (!updates.RulesToAdd[rule].HasFlag(ExceptionBreakpointStates.BreakThrown))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private async Task UpdateCategory(Guid categoryId, ExceptionCategorySettings categorySettings, SettingsUpdates updates)
        {
            // Update the category
            if (updates.NewCategoryState.HasValue && (
                updates.NewCategoryState.Value != ExceptionBreakpointStates.None || // send down a rule if the category isn't in the default state
                categorySettings.CurrentRules.Count != 0)) // Or if we have other rules for the category that we need to blow away
            {
                ExceptionBreakpointStates newCategoryState = updates.NewCategoryState.Value;
                categorySettings.CategoryState = newCategoryState;

                // remove exception breakpoints before categorySettings.CurrentRules is cleared
                await _commandFactory.RemoveExceptionBreakpoint(categoryId, categorySettings.CurrentRules.Values);

                // question: why does categorySettings.CurrentRules need to be cleared?
                categorySettings.CurrentRules.Clear();

                // only do a generic catch throw if C++ exceptions category is checked
                if (IsCppExceptionsCategoryChecked(updates))
                {
                    IEnumerable<ulong> breakpointIds = await _commandFactory.SetExceptionBreakpoints(categoryId, null, newCategoryState);
                    ulong breakpointId = breakpointIds.Single();
                    categorySettings.CurrentRules.Add("*", breakpointId);
                }
            }

            // Process any removes
            if (updates.RulesToRemove.Count > 0)
            {
                // Detach these exceptions from 'CurrentRules'
                List<ulong> breakpointsToRemove = new List<ulong>();
                foreach (string exceptionToRemove in updates.RulesToRemove)
                {
                    ulong breakpointId;
                    if (!categorySettings.CurrentRules.TryGetValue(exceptionToRemove, out breakpointId))
                        continue;

                    categorySettings.CurrentRules.Remove(exceptionToRemove);
                    breakpointsToRemove.Add(breakpointId);
                }

                if (breakpointsToRemove.Count > 0)
                {
                    await _commandFactory.RemoveExceptionBreakpoint(categoryId, breakpointsToRemove);
                }
            }

            // process any adds
            foreach (IGrouping<ExceptionBreakpointStates, string> grouping in updates.RulesToAdd.GroupBy((pair) => pair.Value, (pair) => pair.Key))
            {
                IEnumerable<string> exceptionNames = grouping;

                if (grouping.Key == categorySettings.CategoryState)
                {
                    // A request to set an exception to the same state as the category is redundant unless we have previously changed the state of that exception to something else
                    exceptionNames = exceptionNames.Intersect(categorySettings.CurrentRules.Keys);
                    if (!exceptionNames.Any())
                    {
                        continue; // no exceptions left, so ignore this group
                    }
                }

                bool isBreakThrown = grouping.Key.HasFlag(ExceptionBreakpointStates.BreakThrown);
                if (!IsCppExceptionsCategoryChecked(updates) && isBreakThrown)
                {
                    // IEnumerable<ulong> breakpointIds = await _commandFactory.SetExceptionBreakpoints(categoryId, exceptionNames, grouping.Key);
                    // this approach is very messy, but we need to update categorySettings.CurrentRules
                    foreach (string exceptionName in exceptionNames)
                    {
                        IEnumerable<ulong> breakpointIds = await _commandFactory.SetExceptionBreakpoints(categoryId, new string[] { exceptionName }, grouping.Key);
                        categorySettings.CurrentRules.Add(exceptionName, breakpointIds.First());
                    }
                }
                if (!isBreakThrown)
                {
                    ulong breakpointId;
                    foreach(string exceptionName in exceptionNames)
                    {
                        if (!categorySettings.CurrentRules.TryGetValue(exceptionName, out breakpointId))
                            continue;

                        await _commandFactory.RemoveExceptionBreakpoint(categoryId, new ulong[] { breakpointId });
                        categorySettings.CurrentRules.Remove(exceptionName);
                    }
                }
                
            }
        }

        private string GetExceptionId(string exceptionName, uint dwCode)
        {
            if (dwCode == 0)
                return exceptionName;
            else
                return "0x" + dwCode.ToString("X", CultureInfo.InvariantCulture);
        }

        private ReadOnlyDictionary<Guid, ExceptionCategorySettings> ReadDefaultSettings(HostConfigurationStore configStore)
        {
            Dictionary<Guid, ExceptionCategorySettings> categoryMap = new Dictionary<Guid, ExceptionCategorySettings>();

            IEnumerable<Guid> categories = _commandFactory.GetSupportedExceptionCategories();
            foreach (Guid categoryId in categories)
            {
                string categoryName;
                HostConfigurationSection categoryConfigSection;
                configStore.GetExceptionCategorySettings(categoryId, out categoryConfigSection, out categoryName);

                using (categoryConfigSection)
                {
                    ExceptionCategorySettings categorySettings = new ExceptionCategorySettings(this, categoryConfigSection, categoryName);
                    categoryMap.Add(categoryId, categorySettings);
                }
            }

            return new ReadOnlyDictionary<Guid, ExceptionCategorySettings>(categoryMap);
        }

        public static ExceptionBreakpointStates ToExceptionBreakpointState(enum_EXCEPTION_STATE ad7ExceptionState)
        {
            ExceptionBreakpointStates returnValue = ExceptionBreakpointStates.None;

            if ((ad7ExceptionState & (enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE)) != 0)
                returnValue |= ExceptionBreakpointStates.BreakThrown;
            if (ad7ExceptionState.HasFlag(enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT))
                returnValue |= ExceptionBreakpointStates.BreakUserHandled;

            return returnValue;
        }

        private static bool IsSupportedException(string valueName)
        {
            // For C++, we have a bunch of C++ projection exceptions that we will not want to send down to GDB. Ignore these.
            return valueName.IndexOf('^') < 0;
        }
    }
}
