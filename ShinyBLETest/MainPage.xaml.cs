using Shiny.BluetoothLE;
using System.Linq;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace ShinyBLETest;

#if ANDROID
internal class Android12PlusPermissions : BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new List<(string permissions, bool isRuntime)>
		{
			("android.permission.BLUETOOTH_SCAN", true),
			("android.permission.BLUETOOTH_CONNECT", true)
		}.ToArray();
}
#endif

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void ScanClicked(object sender, EventArgs e)
	{
		var list = await ScanForDeviceList("Z1"); // <--- put the name of the device you want to find and connect to here
		if (list != null && list.Count > 0)
		{
			var peripheral = list[0];
			peripheral.Connect(new ConnectionConfig { AutoConnect = false }); // <--- this will crash on Android
		}
		else
			await DisplayAlert("Information", "No devices found", "OK");
	}

	public async Task<List<IPeripheral>> ScanForDeviceList(string nameContains, int timeout_ms = 5000, bool returnOnFirstFound = false)
	{
#if ANDROID
		// quick and dirty way to get permissions on Android just for this test
		if (await Permissions.CheckStatusAsync<Android12PlusPermissions>() != PermissionStatus.Granted)
		{
			await Permissions.RequestAsync<Android12PlusPermissions>();
		}
#endif
		List<IPeripheral> deviceList = new List<IPeripheral>();

		var bleManager = Shiny.Hosting.Host.Current.Services.GetService<IBleManager>();
		if (bleManager != null)
		{
			if (!bleManager.IsScanning)
			{
				bleManager
					.Scan()
					.Subscribe<ScanResult>((scanResult) =>
					{
						Console.WriteLine($"Scanned device Id: {scanResult?.Peripheral?.Uuid}, Name: {scanResult?.Peripheral?.Name}");

						if (!string.IsNullOrEmpty(scanResult?.Peripheral?.Name))
						{
							if (scanResult.Peripheral.Name.Contains(nameContains))
							{
								if (!deviceList.Where(device => device.Name.Equals(scanResult.Peripheral.Name)).Any())
									deviceList.Add(scanResult.Peripheral);

								if (returnOnFirstFound)
									timeout_ms = 0;
							}
						}
					});

				while (timeout_ms > 0)
				{
					await Task.Delay(100);
					timeout_ms -= 100;
				}

				bleManager.StopScan();
			}
		}

		return deviceList;
	}
}


