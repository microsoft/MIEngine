// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Microsoft.VisualStudio.Debugger.Interop;
using OpenDebug;

namespace OpenDebugAD7
{
    internal class ErrorBuilder
    {
        public readonly Func<string> ScenarioStringFactory;

        public ErrorBuilder(Func<string> scenarioStringFactory)
        {
            ScenarioStringFactory = scenarioStringFactory;
        }

        public void CheckHR(int hr)
        {
            if (hr < 0)
            {
                ThrowHR(hr);
            }
        }

        public void CheckOutput<T>(T value) where T : class
        {
            if (value == null)
            {
                ThrowMissingOutParam();
            }
        }

        public void ThrowHR(int hr)
        {
            throw new AD7Exception(ScenarioStringFactory(), GetErrorDescription(hr));
        }

        public static string GetErrorDescription(int hr)
        {
            // The error messages here are based on the codes that the MIEngine can return plus a few others that seem likely
            // that some other engine would want to return.
            switch (hr)
            {
                case HRConstants.E_NOTIMPL:
                    return new NotImplementedException().Message;

                case HRConstants.COMQC_E_BAD_MESSAGE:
                    return AD7Resources.Msg_COMQC_E_BAD_MESSAGE;

                case HRConstants.RPC_E_SERVERFAULT:
                    return AD7Resources.Msg_RPC_E_SERVERFAULT;

                case HRConstants.RPC_E_DISCONNECTED:
                    return AD7Resources.Msg_RPC_E_DISCONNECTED;

                case HRConstants.E_ACCESSDENIED:
                    return AD7Resources.Msg_E_ACCESSDENIED;

                // The description for these messages are useless, so do what vsdebug does and return nothing for these
                case HRConstants.E_FAIL:
                case HRConstants.E_INVALIDARG:
                    return string.Empty;

                case HRConstants.E_CRASHDUMP_UNSUPPORTED:
                    return AD7Resources.Msg_E_CRASHDUMP_UNSUPPORTED;

                default:
                    return string.Format(CultureInfo.CurrentCulture, AD7Resources.Msg_UnknownError, hr);
            }
        }

        private void ThrowMissingOutParam()
        {
            throw new AD7Exception(ScenarioStringFactory(), AD7Resources.Error_MissingOutParam);
        }

        internal string GetMessageForException(Exception e)
        {
            if (e is AD7Exception)
            {
                return e.Message;
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, ScenarioStringFactory(), Utilities.GetExceptionDescription(e));
            }
        }
    }
}