//using Accenture.MyWizard.Ingrain.DataModels.AICore;
//using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
//using USECASE = Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ICustomDataService
    {
        object TestQueryData(string userId, string clientUID, string deliveryUID, HttpContext httpContext, string category, string ServiceLevel, out bool isError);
        Dictionary<string, string> CheckCustomAPIResponse(string clientUID, string deliveryUID, string userId, HttpContext httpContext, string category);
        void InsertCustomDataSource(CustomDataSourceModel CustomDataSource, string CollectionName);
        void InsertUpdateCustomDataSource(CustomDataSourceModel CustomDataSource, string CollectionName);
        Object GetCustomSourceDetails(string correlationid, string CustomSourceType, string CollectionName);
        string CustomUrlToken(string ApplicationName, AzureDetails oAuthCredentials,string TokenURL);
    }
}
