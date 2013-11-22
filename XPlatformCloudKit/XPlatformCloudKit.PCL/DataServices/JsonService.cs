using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Json;
using XPlatformCloudKit.Models;
using XPlatformCloudKit.Services;

namespace XPlatformCloudKit.DataServices
{
	public class JsonService : IDataService
	{
		HttpClient httpClient = new HttpClient();
		List<Item> JsonData;

		public async Task<List<Item>> GetItems()
		{
			JsonData = new List<Item>();
			Boolean error = false;

			try
			{
				JsonData = new List<Item>();

				List<JsonSource> listJsonSources = new List<JsonSource>();
				listJsonSources.AddRange(AppSettings.JsonAddressCollection.ToList());

				foreach (var jsonSource in listJsonSources)
				{
					await Parse(jsonSource);
				}
			}
			catch { error = true; }

			if (error)
				ServiceLocator.MessageService.ShowErrorAsync("Error when retrieving items from RssService", "Application Error");

			return JsonData;
		}

		public async Task Parse(JsonSource jsonSource)
		{
			var jsonConverter = new MvxJsonConverter();

			var _UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
			httpClient.DefaultRequestHeaders.Add("user-agent", _UserAgent);
			httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

			var response = await httpClient.GetStringAsync(jsonSource.Url);

			IEnumerable<Item> items = jsonConverter.DeserializeObject<List<Item>>(response);

			if (items.ToList().Count > 0)
			{

				foreach (var item in items)
				{
					if (item.Image == null) //Attempt to parse an image out of the description if one is not returned in the RSS
						item.Image = Regex.Match(item.Description, "(https?:)?//?[^'\"<>]+?.(jpg|jpeg|gif|png)").Value;

					//Format dates to look cleaner
					DateTime dateTimeResult = new DateTime();
					if (DateTime.TryParse(item.Subtitle, out dateTimeResult))
						item.Subtitle = dateTimeResult.ToString("ddd, d MMM yyyy");

					JsonData.Add(item);
				};
			}
			else
			{
				await ServiceLocator.MessageService.ShowErrorAsync("Zero items retrieved from " + jsonSource.Url, "Application Error");
			}
		}
	}
}
