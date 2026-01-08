// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Events;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using DebuggerTesting.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class InheritanceTests : TestBase
    {
        #region Constructor

        public InheritanceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForInheritanceTests(ITestSettings settings)
        {
            this.TestPurpose("Compile kitchen sink debuggee for inheritance tests.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Inheritance);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForInheritanceTests))]
        [RequiresTestSettings]
        public void TestSimpleInheritance(ITestSettings settings)
        {
            this.TestPurpose("Test simple inheritance and validate base class members.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Inheritance);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fInheritance");

                this.Comment("Set a line breakpoint in testSimpleInheritance method.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Inheritance, 107));

                this.Comment("Start debugging and break at simple inheritance test.");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    IVariableInspector dog = currentFrame.GetVariable("dog");
                    Assert.NotNull(dog);
                    Assert.Equal("true", dog.GetVariable("isGoodBoy").Value);
                    Assert.Equal("2", dog.GetVariable("barkCount").Value);
                    
                    IVariableInspector dogBase = dog.GetVariable("Animal (base)");
                    Assert.NotNull(dogBase);
                    Assert.Equal("3", dogBase.GetVariable("age").Value);

                    IVariableInspector cat = currentFrame.GetVariable("cat");
                    Assert.NotNull(cat);
                    Assert.Equal("9", cat.GetVariable("lives").Value);
                    Assert.Equal("true", cat.GetVariable("isIndoor").Value);
                    Assert.Equal("1", cat.GetVariable("meowCount").Value);

                    IVariableInspector catBase = cat.GetVariable("Animal (base)");
                    Assert.NotNull(catBase);
                    Assert.Equal("5", catBase.GetVariable("age").Value);

                    IVariableInspector bird = currentFrame.GetVariable("bird");
                    Assert.NotNull(bird);
                    Assert.Equal("true", bird.GetVariable("canFly").Value);
                    Assert.Equal("1", bird.GetVariable("chirpCount").Value);

                    IVariableInspector birdBase = bird.GetVariable("Animal (base)");
                    Assert.NotNull(birdBase);
                    Assert.Equal("2", birdBase.GetVariable("age").Value);
                }

                this.Comment("Run to completion");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForInheritanceTests))]
        [RequiresTestSettings]
        public void TestMultiLevelInheritance(ITestSettings settings)
        {
            this.TestPurpose("Test multi-level inheritance chain and validate all base classes.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Inheritance);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fInheritance");

                this.Comment("Set a line breakpoint in testMultiLevelInheritance method.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Inheritance, 120));

                this.Comment("Start debugging and break at multi-level inheritance test.");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verify Mammal object and its Animal base class.");
                    IVariableInspector mammal = currentFrame.GetVariable("mammal");
                    Assert.NotNull(mammal);

                    // Check Mammal's own members
                    Assert.Equal("true", mammal.GetVariable("hasFur").Value);

                    IVariableInspector mammalBase = mammal.GetVariable("Animal (base)");
                    Assert.NotNull(mammalBase);
                    Assert.Equal("10", mammalBase.GetVariable("age").Value);

                    this.Comment("Verify Pet object with multi-level inheritance (Pet -> Mammal -> Animal).");
                    IVariableInspector pet = currentFrame.GetVariable("pet");
                    Assert.NotNull(pet);
                    Assert.Equal("false", pet.GetVariable("isVaccinated").Value);

                    IVariableInspector petMammalBase = pet.GetVariable("Mammal (base)");
                    Assert.NotNull(petMammalBase);
                    Assert.Equal("true", petMammalBase.GetVariable("hasFur").Value);

                    IVariableInspector petAnimalBase = petMammalBase.GetVariable("Animal (base)");
                    Assert.NotNull(petAnimalBase);
                    Assert.Equal("4", petAnimalBase.GetVariable("age").Value);
                }

                this.Comment("Run to completion");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForInheritanceTests))]
        [RequiresTestSettings]
        public void TestPolymorphicBasePointers(ITestSettings settings)
        {
            this.TestPurpose("Test polymorphism with base class pointers to derived objects.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Inheritance);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fInheritance");

                this.Comment("Set a line breakpoint in testPolymorphism method.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Inheritance, 147));

                this.Comment("Start debugging and break at polymorphism test.");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    IVariableInspector animalPtr1 = currentFrame.GetVariable("animalPtr1");
                    Assert.NotNull(animalPtr1);
                    Assert.Equal("5", animalPtr1.GetVariable("age").Value);

                    IVariableInspector animalPtr2 = currentFrame.GetVariable("animalPtr2");
                    Assert.NotNull(animalPtr2);
                    Assert.Equal("3", animalPtr2.GetVariable("age").Value);

                    IVariableInspector dog1 = currentFrame.GetVariable("dog1");
                    Assert.NotNull(dog1);
                    IVariableInspector dogBase = dog1.GetVariable("Animal (base)");
                    Assert.NotNull(dogBase);
                    Assert.Equal("5", dogBase.GetVariable("age").Value);

                    IVariableInspector cat1 = currentFrame.GetVariable("cat1");
                    Assert.NotNull(cat1);
                    IVariableInspector catBase = cat1.GetVariable("Animal (base)");
                    Assert.NotNull(catBase);
                    Assert.Equal("3", catBase.GetVariable("age").Value);
                }

                this.Comment("Run to completion");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForInheritanceTests))]
        [RequiresTestSettings]
        public void TestTemplateInheritance(ITestSettings settings)
        {
            this.TestPurpose("Test template inheritance with namespaced types containing :: in type names.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Inheritance);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fInheritance");

                this.Comment("Set a line breakpoint in testTemplateInheritance method.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Inheritance, 159));

                this.Comment("Start debugging and break at template inheritance test.");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    IVariableInspector intContainer = currentFrame.GetVariable("intContainer");
                    Assert.NotNull(intContainer);
                    Assert.Equal("42", intContainer.GetVariable("data").Value);
                    Assert.Equal("100", intContainer.GetVariable("capacity").Value);

                    IVariableInspector intAnimalContainer = currentFrame.GetVariable("intAnimalContainer");
                    Assert.NotNull(intAnimalContainer);
                    Assert.Equal("true", intAnimalContainer.GetVariable("isSecure").Value);

                    IVariableInspector intAnimalBase = intAnimalContainer.GetVariable("Animals::Container<int> (base)");
                    Assert.NotNull(intAnimalBase);
                    Assert.Equal("99", intAnimalBase.GetVariable("data").Value);
                    Assert.Equal("200", intAnimalBase.GetVariable("capacity").Value);

                    IVariableInspector doubleAnimalContainer = currentFrame.GetVariable("doubleAnimalContainer");
                    Assert.NotNull(doubleAnimalContainer);

                    IVariableInspector doubleAnimalBase = doubleAnimalContainer.GetVariable("Animals::Container<double> (base)");
                    Assert.NotNull(doubleAnimalBase);
                    Assert.Equal("75", doubleAnimalBase.GetVariable("capacity").Value);
                }

                this.Comment("Run to completion");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
