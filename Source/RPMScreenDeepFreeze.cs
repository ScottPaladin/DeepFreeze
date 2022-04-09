﻿using System;

// This is the recommended method to drive a PropMonitor from a foreign plugin,
// because I'm using it myself and now I have to make sure it works. :)
// This module is both a nice gimmick and an example.
//
// This method has the advantage of letting you make use of the variable processing and you don't
// need to copy a page of boilerplate either -- instead, you just need an InternalModule
// with a method that returns a string. That's it.
//
// RasterPropMonitor module can, beyond the usual types of page read from a text file,
// and defined directly in the config file, request a page text from an InternalModule
// living in the same prop as it does. To do this, configure the page like this:
//
// PAGE
// {
//    PAGEHANDLER
//    {
//        name = Name of your module
//        method = Method name to be called in your module.
//    }
//
// Method name must exist in your module and must be a function that takes two int parameters
// and returns a string. Any other parameters you include in the PAGEHANDLER block
// will be passed to your InternalModule as KSPFields.
//
// RasterPropMonitor will load your module, attach it to the prop it lives in and
// poll this function every time it decides the page needs to be refreshed. You need
// to return a string, that will then be processed just like a page definition text file.
// You will obviously want your own namespace.
namespace DF
{
    // It needs to be an InternalModule.
    public class RPMScreenDeepFreeze : InternalModule
    {
        // These KSPFields will actually be loaded from the PAGEHANDLER block
        [KSPField]
        public string pageTitle;

        // We can keep our response buffered and only return it upon request.
        private string response;

        // We only update the response when the number of lines in the flight log changes.
        private int lastCount = -1;

        // This method will be found by RasterPropMonitorGenerator and called to provide a page.
        // You must return a string. Environment.Newline is the carriage return, nothing fancy.
        public string ShowLog(int screenWidth, int screenHeight)
        {
            if (FlightLogger.eventLog.Count != lastCount)
            {
                LogToBuffer(screenWidth, screenHeight);
            }
            return response;
        }

        // I honestly have no clue why InternalModules need to be initialised
        // like this, and not with a proper constructor or an OnAwake, but that one always works.
        // Even a very simple OnAwake can sometimes get the entire IVA to choke.
        public void Start()
        {
            if (!string.IsNullOrEmpty(pageTitle))
                // Notice that UnMangleConfigText is an extension method defined in JUtil class.
                // To your module, it won't be available without hardlinking to RPM, which is what you want to avoid.
                // It's nothing you couldn't replace with .Replace("<=", "{").Replace("=>", "}").Replace("$$$", Environment.NewLine); though
                pageTitle = pageTitle.Replace("<=", "{").Replace("=>", "}").Replace("$$$", Environment.NewLine);
        }

        // You can have an OnUpdate in this module, this particular one doesn't need it.
        private void LogToBuffer(int screenWidth, int screenHeight)
        {
            // I think I coded this one backwards somehow, but eh, it's a gimmick.
            int activeScreenHeight = screenHeight;
            if (!string.IsNullOrEmpty(pageTitle))
            {
                activeScreenHeight--;
            }
            lastCount = FlightLogger.eventLog.Count;
            if (lastCount > 0)
            {
                string fullLog = Utilities.WordWrap(string.Join(Environment.NewLine, FlightLogger.eventLog.ToArray()), screenWidth);
                var tempBuffer = fullLog.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var screenBuffer = new string[activeScreenHeight];
                if (tempBuffer.Length <= activeScreenHeight)
                {
                    screenBuffer = tempBuffer;
                }
                else
                {
                    for (int i = 0; i < screenBuffer.Length; i++)
                    {
                        screenBuffer[i] = tempBuffer[tempBuffer.Length - activeScreenHeight + i];
                    }
                }
                response = string.Join(Environment.NewLine, screenBuffer);
            }
            else
                response = "No records in log.";
            if (!string.IsNullOrEmpty(pageTitle))
            {
                response = pageTitle + Environment.NewLine + response;
            }
        }
    }
}