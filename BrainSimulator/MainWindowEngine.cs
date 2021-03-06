﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //TODO: Migrate this to a separate object
        static bool engineIsPaused = false;
        static long engineElapsed = 0;
        static long displayElapsed = 0;
        static bool updateDisplay = false;

        static List<int> engineTimerMovingAverage;
        static public void UpdateEngineLabel(int firedCount)
        {
            if (engineTimerMovingAverage == null)
            {
                engineTimerMovingAverage = new List<int>();
                for (int i = 0; i < 100; i++)
                {
                    engineTimerMovingAverage.Add(0);
                }
            }
            engineTimerMovingAverage.RemoveAt(0);
            engineTimerMovingAverage.Add((int)engineElapsed);
            string engineStatus = "Running, Speed: " + thisWindow.slider.Value + "  Cycle: " + theNeuronArray.Generation.ToString("N0") +
            "  " + firedCount.ToString("N0") + " Neurons Fired  " + (engineTimerMovingAverage.Average() / 10000f).ToString("F2") + "ms";
            thisWindow.SetStatus(3, engineStatus, 0);
        }

        private void EngineLoop()
        {
            while (true)
            {
                if (IsArrayEmpty())
                {
                    Thread.Sleep(100);
                }
                else if (engineDelay > 1000)
                {
                    if (updateDisplay)
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            SetStatus(3, "Not Running   Cycle: " + theNeuronArray.Generation.ToString("N0"), 0);
                        });
                        updateDisplay = false;
                        displayUpdateTimer.Start();
                    }
                    Thread.Sleep(100); //check the engineDelay every 100 ms.
                    engineIsPaused = true;
                }
                else
                {
                    engineIsPaused = false;
                    if (theNeuronArray != null)
                    {
                        long start = Utils.GetPreciseTime();
                        theNeuronArray.Fire();
                        long end = Utils.GetPreciseTime();
                        engineElapsed = end - start;

                        if (updateDisplay)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                long dStart = Utils.GetPreciseTime();
                                theNeuronArrayView.UpdateNeuronColors();
                                long dEnd = Utils.GetPreciseTime();
                                displayElapsed = dEnd - dStart;
                            });
                            updateDisplay = false;
                            displayUpdateTimer.Start();
                        }
                    }
                    Thread.Sleep(Math.Abs(engineDelay));
                }
            }
        }

        //
        static Stack<int> engineSpeedStack = new Stack<int>();

        public static void SuspendEngine()
        {
            engineSpeedStack.Push(engineDelay);
            if (theNeuronArray == null)
            {
                return;
            }

            if (theNeuronArray != null)
                theNeuronArray.EngineSpeed = engineDelay;
            engineDelay = 2000;

            //wait for the engine to actually stop before returning
            while (theNeuronArray != null && !engineIsPaused)
            {
                Thread.Sleep(100);
                System.Windows.Forms.Application.DoEvents();
            }
        }

        public static void ResumeEngine()
        {
            engineDelay = engineSpeedStack.Pop();
            if (theNeuronArray == null)
            {
                return;
            }
            //is the engine still paused?
            theNeuronArray.EngineIsPaused = engineDelay == 2000;

            //resume the engine
            //on shutdown, the current applciation may be gone when this is requested
            if (theNeuronArray != null && Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    MainWindow.thisWindow.SetSliderPosition(engineDelay);
                });
            }
        }
    }
}
