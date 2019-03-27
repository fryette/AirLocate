using System;
using Foundation;
using CoreLocation;
using UIKit;
using UserNotifications;
using ObjCRuntime;

namespace AirLocate
{
    public partial class MonitoringViewController : UITableViewController
    {
        bool enabled;
        NSUuid uuid;
        NSNumber major;
        NSNumber minor;
        bool notifyOnEntry;
        bool notifyOnExit;
        bool notifyOnDisplay;

        CLLocationManager locationManger;
        NSNumberFormatter numberFormatter;
        UIBarButtonItem doneButton;
        UIBarButtonItem saveButton;

        public MonitoringViewController (IntPtr handle) : base (handle)
        {
            locationManger = new CLLocationManager {
                Delegate = new CustomDelegate ()
            };

            numberFormatter = new NSNumberFormatter () {
                NumberStyle = NSNumberFormatterStyle.Decimal
            };
            uuid = Defaults.DefaultProximityUuid;
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);
            CLBeaconRegion region = (CLBeaconRegion)locationManger.MonitoredRegions.AnyObject;
            enabled = region != null;

            if (enabled) {
                uuid = region.ProximityUuid;
                major = region.Major;
                minor = region.Minor;
                notifyOnEntry = region.NotifyOnEntry;
                notifyOnExit = region.NotifyOnExit;
                notifyOnDisplay = region.NotifyEntryStateOnDisplay;
            } else {
                uuid = Defaults.DefaultProximityUuid;
                major = minor = null;
                notifyOnEntry = true;
                notifyOnExit = true;
                notifyOnDisplay = false;
            }

            majorTextField.Text = major == null ? string.Empty : major.Int32Value.ToString ();
            minorTextField.Text = minor == null ? string.Empty : minor.Int32Value.ToString ();

            uuidTextField.Text = uuid.AsString ();
            enabledSwitch.On = enabled;
            notifyOnEntrySwitch.On = notifyOnEntry;
            notifyOnExitSwitch.On = notifyOnExit;
            notifyOnDisplaySwitch.On = notifyOnDisplay;
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            uuidTextField.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
            uuidTextField.InputView = new UuidPickerView (uuidTextField);
            uuidTextField.EditingDidBegin += HandleEditingDidBegin;
            uuidTextField.EditingDidEnd += (sender, e) => {
                uuid = new NSUuid (uuidTextField.Text);
                NavigationItem.RightBarButtonItem = saveButton;
            };

            majorTextField.KeyboardType = UIKeyboardType.NumberPad;
            majorTextField.ReturnKeyType = UIReturnKeyType.Done;
            majorTextField.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
            majorTextField.EditingDidBegin += HandleEditingDidBegin;
            majorTextField.EditingDidEnd += (sender, e) => {
                major = numberFormatter.NumberFromString (majorTextField.Text);
                NavigationItem.RightBarButtonItem = saveButton;
            };

            minorTextField.KeyboardType = UIKeyboardType.NumberPad;
            minorTextField.ReturnKeyType = UIReturnKeyType.Done;
            minorTextField.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
            minorTextField.EditingDidBegin += HandleEditingDidBegin;
            minorTextField.EditingDidEnd += (sender, e) => {
                minor = numberFormatter.NumberFromString (minorTextField.Text);
                NavigationItem.RightBarButtonItem = saveButton;
            };

            enabledSwitch.ValueChanged += (sender, e) => {
                enabled = enabledSwitch.On;
            };

            notifyOnEntrySwitch.ValueChanged += (sender, e) => {
                notifyOnEntry = notifyOnEntrySwitch.On;
            };

            notifyOnExitSwitch.ValueChanged += (sender, e) => {
                notifyOnExit = notifyOnExitSwitch.On;
            };

            notifyOnDisplaySwitch.ValueChanged += (sender, e) => {
                notifyOnDisplay = notifyOnDisplaySwitch.On;
            };

            doneButton = new UIBarButtonItem (UIBarButtonSystemItem.Done, (sender, e) => {
                uuidTextField.ResignFirstResponder ();
                majorTextField.ResignFirstResponder ();
                minorTextField.ResignFirstResponder ();
                TableView.ReloadData ();
            });

            saveButton = new UIBarButtonItem (UIBarButtonSystemItem.Save, (sender, e) => {

                foreach (var region in locationManger.MonitoredRegions) {
                    locationManger.StopMonitoring (Runtime.GetNSObject<CLBeaconRegion> (region.Handle));
                }

                if (enabled) {
                    var region = Helpers.CreateRegion (uuid, major, minor);
                    locationManger.RequestAlwaysAuthorization ();
                    var p = CLLocationManager.Status;
                    if (region != null) {
                        region.NotifyOnEntry = notifyOnEntry;
                        region.NotifyOnExit = notifyOnExit;
                        region.NotifyEntryStateOnDisplay = notifyOnDisplay;
                        locationManger.StartMonitoring (region);
                        locationManger.StartRangingBeacons (region);
                        locationManger.RequestState (region);


                    }
                } else {
                    var region = (CLBeaconRegion)locationManger.MonitoredRegions.AnyObject;
                    if (region != null)
                        locationManger.StopMonitoring (region);
                }
                //NavigationController.PopViewController (true);
            });

            NavigationItem.RightBarButtonItem = saveButton;
        }

        // identical code share across all UITextField
        void HandleEditingDidBegin (object sender, EventArgs e)
        {
            NavigationItem.RightBarButtonItem = doneButton;
        }

        private class CustomDelegate : CLLocationManagerDelegate
        {
            public override void DidVisit (CLLocationManager manager, CLVisit visit)
            {
                UNUserNotificationCenter.Current.RequestAuthorization (UNAuthorizationOptions.Alert, (arg1, arg2) => { });
                var notification = new UNMutableNotificationContent {
                    Title = "Did visit",
                    Subtitle = "Something This Way Comes",
                    Body = "I need to tell you something, but first read this."
                };

                var notificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger (5, false);
                var request = UNNotificationRequest.FromIdentifier ("notification1", notification, notificationTrigger);
                UNUserNotificationCenter.Current.AddNotificationRequest (request, null);
            }

            public override void LocationsUpdated (CLLocationManager manager, CLLocation [] locations)
            {
            }

            public override void DidRangeBeacons (CLLocationManager manager, CLBeacon [] beacons, CLBeaconRegion region)
            {
            }

            public override void RegionLeft (CLLocationManager manager, CLRegion region)
            {
                UNUserNotificationCenter.Current.RequestAuthorization (UNAuthorizationOptions.Alert, (arg1, arg2) => { });
                var notification = new UNMutableNotificationContent {
                    Title = "Region Left",
                    Subtitle = "Something This Way Comes",
                    Body = "I need to tell you something, but first read this."
                };

                var notificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger (5, false);
                var request = UNNotificationRequest.FromIdentifier ("notification1", notification, notificationTrigger);
                UNUserNotificationCenter.Current.AddNotificationRequest (request, null);
            }

            public override void RegionEntered (CLLocationManager manager, CLRegion region)
            {
                UNUserNotificationCenter.Current.RequestAuthorization (UNAuthorizationOptions.Alert, (arg1, arg2) => { });
                var notification = new UNMutableNotificationContent {
                    Title = "Region entered",
                    Subtitle = "Something This Way Comes",
                    Body = "I need to tell you something, but first read this."
                };

                var notificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger (5, false);
                var request = UNNotificationRequest.FromIdentifier ("notification1", notification, notificationTrigger);
                UNUserNotificationCenter.Current.AddNotificationRequest (request, null);
            }

            public override void DidStartMonitoringForRegion (CLLocationManager manager, CLRegion region)
            {
            }

            public override void MonitoringFailed (CLLocationManager manager, CLRegion region, NSError error)
            {
            }

            public override void DidDetermineState (CLLocationManager manager, CLRegionState state, CLRegion region)
            {
            }
        }
    }
}
