using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
   public class GenericInstaMLTestData
    {
        public string UseCaseID { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string CreatedByUser { get; set; }
        public string ProcessName { get; set; }
        public List<MonthlySales> ActualData { get; set; }
      //  public List<IntsaMLTrainingDataResponse> IntsaMLDataResponse { get; set; }
    }
    public class IntsaMLTrainingDataResponse
    {
        public string InstaID { get; set; }
        public List<MonthlySales> ActualData { get; set; }
        public string PredictedData { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }

    }



    public class Insurance
    {
        //age	sex	bmi	children	smoker	region	charges

        public string Age { get; set; }
        public string Sex { get; set; }
        public string Children { get; set; }
        public string Bmi { get; set; }
        public string Region { get; set; }
        public string Charges { get; set; }

        public string Smoker { get; set; }

        public Insurance(string age, string sex, string children, string smoker, string bmi, string region, string charges)
        {
            Age = age;
            Sex = sex;
            Children = children;
            Smoker = smoker;
            Bmi = bmi;
            Region = region;
            Charges = charges;
        }
    }

    public class MonthlySales
    {
        public string Date { get; set; }
        public string value { get; set; }
        public MonthlySales(string date, string sales)
        {
            Date = date;
            value = sales;
        }
    }
}
