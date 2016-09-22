using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Win10Unlocker.Server.BLL
{
    public class BluetoothListener : IDisposable
    {
        public event EventHandler<string> MessageReceived;

        // The background task registration for the background advertisement watcher
        private IBackgroundTaskRegistration taskRegistration;
        // The watcher trigger used to configure the background task registration
        private BluetoothLEAdvertisementWatcherTrigger trigger;
        // A name is given to the task in order for it to be identifiable across context.
        private string taskName = "Bluetooth_BackgroundTask";
        // Entry point for the background task.
        private string taskEntryPoint = "Tasks.Server.AdvertisementWatcherTask";

        public BluetoothListener()
        {
            // Create and initialize a new trigger to configure it.
            trigger = new BluetoothLEAdvertisementWatcherTrigger();

            // Configure the advertisement filter to look for the data advertised by the publisher in Scenario 2 or 4.
            // You need to run Scenario 2 on another Windows platform within proximity of this one for Scenario 3 to 
            // take effect.

            // Unlike the APIs in Scenario 1 which operate in the foreground. This API allows the developer to register a background
            // task to process advertisement packets in the background. It has more restrictions on valid filter configuration.
            // For example, exactly one single matching filter condition is allowed (no more or less) and the sampling interval

            // For determining the filter restrictions programatically across APIs, use the following properties:
            //      MinSamplingInterval, MaxSamplingInterval, MinOutOfRangeTimeout, MaxOutOfRangeTimeout

            // Part 1A: Configuring the advertisement filter to watch for a particular advertisement payload

            // First, let create a manufacturer data section we wanted to match for. These are the same as the one 
            // created in Scenario 2 and 4. Note that in the background only a single filter pattern is allowed per trigger.
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = 0xFFFE;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            DataWriter writer = new DataWriter();
            writer.WriteUInt16(0x1234);

            // Make sure that the buffer length can fit within an advertisement payload. Otherwise you will get an exception.
            manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the trigger:
            trigger.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            // Part 1B: Configuring the signal strength filter for proximity scenarios

            // Configure the signal strength filter to only propagate events when in-range
            // Please adjust these values if you cannot receive any advertisement 
            // Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
            // will start to be considered "in-range".
            trigger.SignalStrengthFilter.InRangeThresholdInDBm = -70;

            // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
            // to determine when an advertisement is no longer considered "in-range"
            trigger.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;

            // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
            // to determine when an advertisement is no longer considered "in-range"
            trigger.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);

            // By default, the sampling interval is set to be disabled, or the maximum sampling interval supported.
            // The sampling interval set to MaxSamplingInterval indicates that the event will only trigger once after it comes into range.
            // Here, set the sampling period to 1 second, which is the minimum supported for background.
            trigger.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(1000);

            InitializeTask();
        }

        private void InitializeTask()
        {
            if (taskRegistration == null)
            {
                // Find the task if we previously registered it
                foreach (var task in BackgroundTaskRegistration.AllTasks.Values)
                {
                    if (task.Name == taskName)
                    {
                        taskRegistration = task;
                        taskRegistration.Completed += OnBackgroundTaskCompleted;
                        break;
                    }
                }
            }
            else
            {
                taskRegistration.Completed += OnBackgroundTaskCompleted;
            }
        }

        public async Task RegisterTask()
        {
            // Registering a background trigger if it is not already registered. It will start background scanning.
            // First get the existing tasks to see if we already registered for it
            if (taskRegistration != null)
            {
                return;
            }
            else
            {
                // Applications registering for background trigger must request for permission.
                BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
                // Here, we do not fail the registration even if the access is not granted. Instead, we allow 
                // the trigger to be registered and when the access is granted for the Application at a later time,
                // the trigger will automatically start working again.

                // At this point we assume we haven't found any existing tasks matching the one we want to register
                // First, configure the task entry point, trigger and name
                var builder = new BackgroundTaskBuilder();
                builder.TaskEntryPoint = taskEntryPoint;
                builder.SetTrigger(trigger);
                builder.Name = taskName;

                // Now perform the registration. The registration can throw an exception if the current 
                // hardware does not support background advertisement offloading
                try
                {
                    taskRegistration = builder.Register();

                    // For this scenario, attach an event handler to display the result processed from the background task
                    taskRegistration.Completed += OnBackgroundTaskCompleted;
                }
                catch (Exception ex)
                {
                    switch ((uint)ex.HResult)
                    {
                        case (0x80070032): // ERROR_NOT_SUPPORTED                            
                            break;
                        default:
                            throw ex;
                    }
                }
            }
        }

        /// <summary>
        /// Handle background task completion.
        /// </summary>
        /// <param name="task">The task that is reporting completion.</param>
        /// <param name="e">Arguments of the completion report.</param>
        private void OnBackgroundTaskCompleted(BackgroundTaskRegistration task, BackgroundTaskCompletedEventArgs eventArgs)
        {
            // We get the advertisement(s) processed by the background task
            if (ApplicationData.Current.LocalSettings.Values.Keys.Contains(taskName))
            {
                var backgroundMessage = (string)ApplicationData.Current.LocalSettings.Values[taskName];
                OnMessageReceived(backgroundMessage);                         
            }
        }

        private void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        public void Dispose()
        {
            taskRegistration.Completed -= OnBackgroundTaskCompleted;                       
        }
    }
}
