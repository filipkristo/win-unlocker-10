using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Win10Unlocker.Client.ViewModels;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;

namespace Win10Unlocker.Client.Model
{
    public class DeviceInformationDisplay : BindableBase
    {
        private DeviceInformation deviceInfo;

        #region Properties

        public DeviceInformationKind Kind
        {
            get
            {
                return deviceInfo.Kind;
            }
        }

        public string Id
        {
            get
            {
                return deviceInfo.Id;
            }
        }

        public string Name
        {
            get
            {
                return deviceInfo.Name;
            }
        }

        public BitmapImage GlyphBitmapImage
        {
            get;
            private set;
        }

        public bool CanPair
        {
            get
            {
                return deviceInfo.Pairing.CanPair;
            }
        }

        public bool IsPaired
        {
            get
            {
                return deviceInfo.Pairing.IsPaired;
            }
        }

        public IReadOnlyDictionary<string, object> Properties
        {
            get
            {
                return deviceInfo.Properties;
            }
        }

        public DeviceInformation DeviceInformation
        {
            get
            {
                return deviceInfo;
            }

            private set
            {
                deviceInfo = value;
            }
        }

        #endregion

        public DeviceInformationDisplay(DeviceInformation deviceInfoIn)
        {
            deviceInfo = deviceInfoIn;
            UpdateGlyphBitmapImage();
        }


        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            deviceInfo.Update(deviceInfoUpdate);
            
            RaisePropertyChanged("Kind");
            RaisePropertyChanged("Id");
            RaisePropertyChanged("Name");
            RaisePropertyChanged("DeviceInformation");
            RaisePropertyChanged("CanPair");
            RaisePropertyChanged("IsPaired");

            UpdateGlyphBitmapImage();
        }

        private async void UpdateGlyphBitmapImage()
        {
            var deviceThumbnail = await deviceInfo.GetGlyphThumbnailAsync();
            var glyphBitmapImage = new BitmapImage();
            await glyphBitmapImage.SetSourceAsync(deviceThumbnail);
            GlyphBitmapImage = glyphBitmapImage;
            RaisePropertyChanged("GlyphBitmapImage");
        }
    }
}
