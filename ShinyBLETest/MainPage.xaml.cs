using Shiny.BluetoothLE;
using System.Linq;

namespace ShinyBLETest;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void ScanClicked(object sender, EventArgs e)
	{
		var list = await ScanForDeviceList("Z1");
		if (list != null && list.Count > 0)
			await DisplayAlert("Information", $"Found {list.Count} devices", "OK");
		else
			await DisplayAlert("Information", "No devices found", "OK");
	}

	public async Task<List<IPeripheral>> ScanForDeviceList(string nameContains, int timeout_ms = 5000, bool returnOnFirstFound = false)
	{
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


