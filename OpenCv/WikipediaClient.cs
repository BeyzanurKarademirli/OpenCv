using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace OpenCv
{
    /// <summary>
    /// Türkçe Vikipedi'den ücretsiz (API anahtarı gerektirmeyen) makale özeti getirir.
    /// İnternet yoksa uygulama çalışmaya devam eder, sadece özet gösterilmez.
    /// </summary>
    internal static class WikipediaClient
    {
        private const string SummaryUrl = "https://tr.wikipedia.org/api/rest_v1/page/summary/";

        private static readonly HttpClient Http;
        private static readonly ConcurrentDictionary<string, string> Cache =
            new ConcurrentDictionary<string, string>();

        static WikipediaClient()
        {
            // .NET Framework 4.7.2 varsayılan olarak TLS 1.2'yi açmayabilir.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            Http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            // Vikipedi API'si tanımlayıcı bir User-Agent ister.
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("OpenCvKamera/1.0 (egitim projesi)");
        }

        /// <summary>Özet metni; bulunamazsa null döner. Ağ hatalarında istisna fırlatır.</summary>
        public static async Task<string> GetSummaryAsync(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;

            string cached;
            if (Cache.TryGetValue(title, out cached)) return cached;

            string url = SummaryUrl + Uri.EscapeDataString(title.Replace(' ', '_'));
            using (HttpResponseMessage response = await Http.GetAsync(url).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Cache[title] = null;
                    return null;
                }

                response.EnsureSuccessStatusCode();
                byte[] json = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                var serializer = new DataContractJsonSerializer(typeof(Summary));
                Summary summary;
                using (var stream = new MemoryStream(json))
                    summary = (Summary)serializer.ReadObject(stream);

                // Anlam ayrımı sayfaları ("... şunlara işaret edebilir") işe yaramaz
                string extract = summary != null && summary.Type != "disambiguation" ? summary.Extract : null;
                Cache[title] = extract;
                return extract;
            }
        }

        [DataContract]
        private sealed class Summary
        {
            [DataMember(Name = "type")]
            public string Type { get; set; }

            [DataMember(Name = "extract")]
            public string Extract { get; set; }
        }
    }
}
