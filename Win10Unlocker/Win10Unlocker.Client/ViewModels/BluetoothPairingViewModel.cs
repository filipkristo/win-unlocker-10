using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Common;
using Win10Unlocker.Client.Model;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Xaml.Navigation;

namespace Win10Unlocker.Client.ViewModels
{
    public class BluetoothPairingViewModel : BaseViewModel
    {        
        private const string bluetoothSelector = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";
        private const DeviceInformationKind bluetoothKind = DeviceInformationKind.AssociationEndpoint;

        private DeviceWatcher deviceWatcher = null;

        public ObservableCollection<DeviceInformationDisplay> ResultCollection { get; private set; } = new ObservableCollection<DeviceInformationDisplay>();        

        private TypedEventHandler<DeviceWatcher, DeviceInformation> handlerAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerRemoved = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerEnumCompleted = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerStopped = null;

        public bool PairButtonEnabled { get { return Get<bool>(); } set { Set(value); } }
        public bool UnPairButtonEnabled { get { return Get<bool>(); } set { Set(value); } }
        public DeviceInformationDisplay SelectedItem { get { return Get<DeviceInformationDisplay>(); } set { Set(value); } }

        public BluetoothPairingViewModel()
        {
                     
        }

        private void Initialize()
        {            
            deviceWatcher = DeviceInformation.CreateWatcher(
                    bluetoothSelector,
                    null, 
                    bluetoothKind);            

            handlerAdded = new TypedEventHandler<DeviceWatcher, DeviceInformation>((watcher, deviceInfo) =>
            {
                var exists = ResultCollection.ToList().Exists(x => x.Id == deviceInfo.Id);
                if (exists)
                    return;

                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    ResultCollection.Add(new DeviceInformationDisplay(deviceInfo));
                });

            });
            deviceWatcher.Added += handlerAdded;

            handlerUpdated = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                var update = ResultCollection.Where(x => x.Id == deviceInfoUpdate.Id).FirstOrDefault();

                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    update.Update(deviceInfoUpdate);
                    UpdatePairingButtons();
                });                
                
            });
            deviceWatcher.Updated += handlerUpdated;

            handlerRemoved = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                var deleted = ResultCollection.Where(x => x.Id == deviceInfoUpdate.Id).FirstOrDefault();                

                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    ResultCollection.Remove(deleted);
                });

            });
            deviceWatcher.Removed += handlerRemoved;

            handlerEnumCompleted = new TypedEventHandler<DeviceWatcher, Object>((watcher, obj) =>
            {                
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    Status = $"{ResultCollection.Count} devices found. Enumeration completed. Watching for updates...";
                });

            });
            deviceWatcher.EnumerationCompleted += handlerEnumCompleted;

            handlerStopped = new TypedEventHandler<DeviceWatcher, Object>((watcher, obj) =>
            {
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    Status = $"{ResultCollection.Count} devices found. Watcher {(DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped")}.";
                });
                                                        
            });
            deviceWatcher.Stopped += handlerStopped;

            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                Status = "Discovering bluetooth devices...";
            });            

            deviceWatcher.Start();            
        }

        private void UpdatePairingButtons()
        {
            var deviceInfoDisp = SelectedItem;

            if (null != deviceInfoDisp &&
                deviceInfoDisp.DeviceInformation.Pairing.CanPair &&
                !deviceInfoDisp.DeviceInformation.Pairing.IsPaired)
            {
                PairButtonEnabled = true;
            }
            else
            {
                PairButtonEnabled = false;
            }

            if (null != deviceInfoDisp &&
                deviceInfoDisp.DeviceInformation.Pairing.IsPaired)
            {
                UnPairButtonEnabled = true;
            }
            else
            {
                UnPairButtonEnabled = false;
            }
        }

        private void StopWatching()
        {
            if (deviceWatcher != null)
            {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                deviceWatcher.Added -= handlerAdded;
                deviceWatcher.Updated -= handlerUpdated;
                deviceWatcher.Removed -= handlerRemoved;
                deviceWatcher.EnumerationCompleted -= handlerEnumCompleted;

                if (DeviceWatcherStatus.Started == deviceWatcher.Status ||
                    DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status)
                {
                    deviceWatcher.Stop();
                }
            }
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Initialize();

            await Task.CompletedTask;
        }

        public override async Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            StopWatching();

            await Task.CompletedTask;
        }
    }
}
