using System;
using System.Collections.Generic;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    interface ITrainGenericModels
    {
        void FetchListOfClients();
        void FetchListofDeliveryConstructs();
        List<WINSERVICEMODELS.DeliveryConstructDetails> GetDeliveryConstructsListsFromDB();
        Task<List<WINSERVICEMODELS.ProductDetails>> CheckProductConfigurationForDeliveryConstruct(string clientUId,string deliveryConstructUId);
        string UpdateProductConfiginDB(string clientUId, string deliveryConstructUId, List<WINSERVICEMODELS.ProductDetails> prodDetailsLst);
        void VerifyIfModelTrainedForDC();      
        List<WINSERVICEMODELS.AutoTraintask> CheckAutoTraintaskStatus();

        bool CreateAutoTrainTask();
        void UpdateProductConfigStatus();
        void TrainModels();
        WINSERVICEMODELS.AutoTraintask GetTaskStatus(string taskCode);
        void UpdateDataSetsWithIncrementalData();

    }
}
