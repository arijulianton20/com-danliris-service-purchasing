using Com.DanLiris.Service.Purchasing.Lib.Models.Expedition;
using Com.DanLiris.Service.Purchasing.Test.DataUtils.ExpeditionDataUtil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Com.DanLiris.Service.Purchasing.Test.Controllers.Expedition
{
   // [Collection("TestServerFixture Collection")]
    public class PurchasingDocumentExpeditionTest
    {
        //private const string MediaType = "application/json";
        //private readonly string URI = "v1/expedition/purchasing-document-expeditions";

        //private TestServerFixture TestFixture { get; set; }

        //private HttpClient Client
        //{
        //    get { return this.TestFixture.Client; }
        //}

        //protected SendToVerificationDataUtil DataUtil
        //{
        //    get { return (SendToVerificationDataUtil)this.TestFixture.Service.GetService(typeof(SendToVerificationDataUtil)); }
        //}

        //public PurchasingDocumentExpeditionTest(TestServerFixture fixture)
        //{
        //    TestFixture = fixture;
        //}

        //[Fact]
        //public async Task Should_Success_Get_All_Data()
        //{
        //    var response = await this.Client.GetAsync(URI);
        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //    var json = await response.Content.ReadAsStringAsync();
        //    Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json.ToString());

        //    Assert.True(result.ContainsKey("apiVersion"));
        //    Assert.True(result.ContainsKey("message"));
        //    Assert.True(result.ContainsKey("data"));
        //    Assert.Equal("JArray", result["data"].GetType().Name);
        //}

        //[Fact]
        //public async Task Should_Success_Get_Data_By_Id()
        //{
        //    PurchasingDocumentExpedition Model = await DataUtil.GetTestData();

        //    var response = await this.Client.GetAsync(string.Concat(URI, "/", Model.Id));
        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //    var json = await response.Content.ReadAsStringAsync();
        //    Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json.ToString());

        //    Assert.True(result.ContainsKey("apiVersion"));
        //    Assert.True(result.ContainsKey("message"));
        //    Assert.True(result.ContainsKey("data"));
        //    Assert.Equal("JObject", result["data"].GetType().Name);
        //}

        //[Fact]
        //public async Task Should_Success_Delete_Data()
        //{
        //    PurchasingDocumentExpedition Model = await DataUtil.GetTestData();

        //    var response = await this.Client.DeleteAsync(string.Concat(URI, "/", Model.Id));
        //    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        //}

        //[Fact]
        //public async Task Should_Success_Delete_Data_By_UPO_No()
        //{
        //    PurchasingDocumentExpedition Model = await DataUtil.GetTestData();

        //    var response = await this.Client.DeleteAsync(string.Concat(URI, "/PDE/", Model.UnitPaymentOrderNo));
        //    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        //}
    }
}
