import { Injectable } from '@angular/core';


@Injectable({
  providedIn: 'root'
})
export class InfoTextService {
  dateTypeIconInfo = {
    'Data Volume Analysis': `Comparison of data inflow trend across time period (Year Quarter – Year Quarter; Year Month – Year Month)`,
    'Trend Forecast': `Data inflow is predicted for the frequency provided by the user (Daily, Weekly, Monthly, Quarterly) `,
    'Correlated Column Analysis': `inferences on data volume is generated based on the attributes selected by the user`,
    'DateColumnList': `Date Attribute for generating data inflow forecasts and past trends`,
    'TrendForeCast': `Predicting future data inflow trend`,
    'Frequency': `Select the required frequencies(Daily/Weekly/Monthly/Quarterly) for Trend Forecasts`,
    'AttributeList': `Selected attributes will be used to generate inferences on data volume`,

     'Data Inflow Analysis_Accordian': `Comparison of trend in data volume across two given time period (Year quarter – Year quarter, Year month – Year month. Eg: June 2018 & June 2019; it does not include the period between June 2018 & June 2019. It only compares between these two months alone`,
     'Data Volume Analysis_Accordian': `Comparison of trend in data volume across two given time period (Year quarter – Year quarter, Year month – Year month. Eg: June 2018 & June 2019; it does not include the period between June 2018 & June 2019. It only compares between these two months alone`,
     'Trend Forecast_Accordian': `Based on the frequency provided by the user, forecast of the data inflow will be generated`,
     'Correlated Column Analysis_Accordian': `Based on the attributes selected by the user, inferences for data inflow will be generated`,
     'Distributive Analysis_Accordian': `Comparison of trend in data volume across two given period (Year quarter- Year quarter, Year month- Year month. Eg: June 2018 & June 2019; it does not include the period/months between June 2018 & June 2019. It only compares between these two months alone)`,
     'Distributive Narratives_Accordian': `Based on the attributes selected by the user, inferences on distribution of the data is generated`,
     'Distributive Narratives': `Inferences on Data Volume is generated based on the attributes selected by the user`,
     'Measure Analysis':`Inferences on Measure is generated based on the attributes selected by the user`
  }


  measureIconInfo = {
    'Measure Analysis': `Inferences on Measure is generated based on the attributes selected by the user`,
    'Outlier Analysis': `Inferences on Measure is generated based on the attribute combinations selected by the user`,
    'DateColumnList': `Date-parts of the selected date will be utilized to generate inferences on Measure`,
    'MetricColumnList': `Attribute based on which inferences will be generated`,
    'AttributeList': `System generated top 15 (or less depending on the total number of columns in the data) attributes influencing the selected measure are by default considered. Please deselect the ones that are not required for your analysis`,
    'AttributeCombinationList': `System generated top 15 (or less depending on the total number of columns in the data) attribute combinations influencing the selected measure are by default considered. Please deselect the ones that are not required for your analysis`,
    'Filters': `Select specific categories if required. The filtered data would be considered for generating inferences on Measure`,
    
    'Measure Analysis_Accordian': `Based on the correlation of the important attributes & attribute combinations selected by the user, inferences for the measure will be generated`,
    'Outlier Analysis_Accordian': `Based on the correlation of the important attributes & attribute combinations selected by the user, outliers in inferences for the measure will be displayed`
  }

  constructor() { }

}

