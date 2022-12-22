# -*- coding: utf-8 -*-
"""
Created on Tue Aug 31 11:38:38 2021

@author: maddikuntla.p.kumar
"""

from datetime import datetime

import pandas as pd
import numpy as np
#from pandas import *
import numpy as np
# from libraries.settings import *
from scipy.stats.stats import pearsonr
import itertools
import statsmodels.api as sm 
from statsmodels.formula.api import ols
import xlrd
from scipy import stats 
import json
import dateutil.parser
from SSAIutils import utils
from SSAIutils.inference_engine_utils import *
#from MongoDBConnection import *
from SSAIutils import EncryptData


'''
    correlationId = '13072021-storydata'   # '08062021-storydata' #'04062021-ioincidentdata'
    requestId = 'bd153ee9-ba92-44c1-bbb7-3593563818d2'
    InferenceConfigId = 'InferenceConfigId'
    InferenceConfigType='VolumetricAnalysis'   
    pageInfo = 'GenerateVolumetric'
    userId = 'praveen kumar'
    
'''



def generateVolumetricPageInfo(correlationId,requestId,InferenceConfigId,pageInfo,userId,EncryptionFlag,logger):
            """
            ##############to be changed during template configuration    
            entity ='ticket'  #IE_Model  using  corrId
            ##############
            
    
            ######################################################
            #IMPORTANT - to be discussed with API
            datefromUser='Reported Date'
            #DB connection
            dbconn,dbcollection = utils.open_dbconn('IE_Config')
            data_json = dbcollection.find(
            { "CorrelationId" :correlationId,
            },{"DimensionsList":1})
            
            k=list(data_json)[0]
            selecteddims=k.get('DimensionsList')
            dbconn.close()
            
            #update_volumetric_Config(CorrelationId, datefromUser, selecteddims, "True", ['daily','monthly','quarterly','weekly'],userId)
            
            utils.saveConfigInDB(correlationId, InferenceConfigId, 'VolumetricAnalysis', 'config1', 'praveenkumar', datefromUser, 'True', ['daily','monthly','quarterly','weekly'],selecteddims)
            #########################################
            """
            utils.logger(logger, correlationId, 'INFO', 'GenerateVolumetric Invoked',str(requestId))

            dbconn, dbcollection = utils.open_dbconn('IE_RequestQueue')
            args = dbcollection.find_one({"CorrelationId": correlationId, "RequestId": requestId})
            InferenceConfigType = args['InferenceConfigType']

            utils.updQdb(correlationId, 'P', '5', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
            
            entity=utils.getEntityFromDB(correlationId)
            
            if entity == "":
                entity='entity data'
            pluralentity=getPluralForEntity(entity.lower())
            
            date_by_user,imp_columns,trend_forecasting,trend_flag=utils.get_volumetric_config(correlationId,InferenceConfigId,InferenceConfigType)   #CorrId,InferenceConfigId,InferenceConfigType
            #changes start##
            
            # data = utils.data_from_chunks(corid=correlationId,collection="IE_IngestData")
            # data = data.dropna(how='all')
            # threshold=int(len(data)*0.02)
            # data_copy,unique_values_dict,null_value_columns=get_uniqueValues(data)
            # databins,updatedUniquedict,bincolumns,numericalcolumns=binningContinousColumn(data_copy,unique_values_dict,threshold)
            
            databins = utils.data_from_chunks(corid=correlationId,collection="IE_PreprocessedData")
            bincolumns,numericalcolumns=utils.getBinAndNumericalColumnsFromDB(correlationId)
            databins=formatBinValues(databins,bincolumns)            
            dataforVolumetric=databins.dropna(subset=[date_by_user])
            
            utils.updQdb(correlationId, 'P', '10', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            #####changes###
            dataforVolumetric[date_by_user] = dataforVolumetric[date_by_user].dt.strftime('%d-%m-%Y %H:%M:%S')
            ####end####
            
            #data_with_year_month_day_quater
            data_with_ymdq,date_fortrend,imp_columns=add_datecolumn_by_user(dataforVolumetric,imp_columns,date_by_user)
    
            #creating a DF of selected columns which will be used for further processing - wring it to a excel for reference
            selected_data=data_with_ymdq.loc[:, data_with_ymdq.columns.isin(imp_columns)]
            
            #excluding if there is any date columns
            selected_data = selected_data.select_dtypes(exclude=['datetime64[ns]', 'datetime64[ns, UTC]','datetime64[ns, tzlocal()]'])
            imp_columns=list(selected_data.columns)
            
            #replace all empty values with hardcoded text <Blank> so that we can give some analysis on the blank values
            selected_data= replace_null_values(selected_data)
    
            #selected_data.to_excel("selectedData.xlsx")
    
            #creating a slice of data at every ImpColumns with counters  
            selected_data_counter = selected_data.groupby(imp_columns).size().reset_index(name = 'counts')
            utils.updQdb(correlationId, 'P', '20', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            #selected_data_counter.to_excel("selectedDataCounter.xlsx")
    
            year=date_by_user+"_Year"  #year column in selected_data_counter
            quarter=date_by_user+"_Quarter" #quarter column in selected_data_counter
            month=date_by_user+"_Month"  #month column in selected_data_counter
            day=date_by_user+'_Day'  #day column in selected_data_counter
            weekday=date_by_user+'_Weekday'
            hour=date_by_user+'_Hour'
            
            volumetric_narratives={}
    
            # =============================================================================
            # #generating narratives (time lapse)
            # =============================================================================
            timelapse_narratives_dict={'yearly_narratives':None,'quarterly_narratives':None,'monthly_narratives':None}
            #year wide analysis
            yearly_narratives,year_df=getNarrativesByDate(selected_data_counter,entity,year)
            ##  **give blank narratives   confirmed by Ashwar**
            yearly_narratives=None
            ## 
            utils.updQdb(correlationId, 'P', '30', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            if yearly_narratives is not None:    
                timelapse_narratives_dict['yearly_narratives']=yearly_narratives.to_dict('records')
                #yearly_narratives.to_excel('yearly_narratives.xlsx')
            #year & Quarter wide analysis
            quarterly_narratives,quarter_df=getNarrativesByDate(selected_data_counter,entity,year,quarter)    
            if quarterly_narratives is not  None:    
                timelapse_narratives_dict['quarterly_narratives']=quarterly_narratives.to_dict('records')
                #quarterly_narratives.to_excel('quarterly_narratives.xlsx')
            else:
                timelapse_narratives_dict['quarterly_narratives']=TextForNoQuarterNarratives()
    
            #year & month wide analysis
            monthly_narratives,month_df=getNarrativesByDate(selected_data_counter,entity,year,month)    
            if monthly_narratives is not None:        
                #monthly_narratives.to_excel('monthly_narratives.xlsx')
                timelapse_narratives_dict['monthly_narratives']=monthly_narratives.to_dict('records')
            else:
                timelapse_narratives_dict['monthly_narratives']=TextForNoMonthNarratives()
                
            volumetric_narratives['inflow_narratives']=timelapse_narratives_dict
            utils.updQdb(correlationId, 'P', '50', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            month_list = ['January', 'February' ,'March' ,'April','May' ,'June' ,'July' ,'August','September', 'October' ,'November' ,'December']
                                                                                                                                    
           # Trend analysis:
            trend_prediction={'prediction_day':None,'prediction_week':None,'prediction_month':None,'prediction_quarter':None}
            if str(trend_forecasting) in ["True","yes"]:
                #get day_df  using date given by user and selected Data
                day_df,week_df=get_day_df(date_fortrend, date_by_user)
                if 'daily' in trend_flag:
                    if len(day_df)>2:
                        day_pred = get_Date_Prediction(day_df,pluralentity,month_list)
                        trend_prediction['prediction_day']=day_pred.to_dict('records')
                    else:
                        trend_prediction['prediction_day']=TextForNoDayPredictionNarratives()
                if 'weekly' in trend_flag:
                    if len(week_df)>2:
                        week_pred = get_Week_Prediction(week_df,pluralentity,month_list)
                        trend_prediction['prediction_week']=week_pred.to_dict('records')
                    else:
                        trend_prediction['prediction_week']=TextForNoWeekPredictionNarratives()
                        
                if 'monthly' in trend_flag:
                    if len(month_df)>2:
                        month_pred = get_Month_Prediction(month_df,pluralentity)
                        trend_prediction['prediction_month']=month_pred.to_dict('records')
                    else:
                        trend_prediction['prediction_month']=TextForNoMonthPredictionNarratives()

                        
                if 'quarterly' in trend_flag:
                    if len(quarter_df)>2:      
                        quarter_pred = get_Quarter_Prediction(quarter_df,pluralentity)
                        trend_prediction['prediction_quarter']=quarter_pred.to_dict('records')
                    else:
                        trend_prediction['prediction_quarter']=TextForNoQuarterPredictionNarratives()

                        
                        
                # if 'yearly'in trend_flag:
                #     if len(year_df)>2:            
                #         yearly_pred = get_Year_Prediction(year_df,pluralentity)
                #         trend_prediction['prediction_yearly']=yearly_pred.to_dict('records')
    
       
            volumetric_narratives['trend_forecasting']=trend_prediction
            utils.updQdb(correlationId, 'P', '70', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
    
            # =============================================================================
            # end for trend analysis
            # 
            # =============================================================================
    
            #encoding month ,weekday,quarter  
            selected_data_counter_EncodeDateparts=encode_date_columns_volumetricAnalysis(selected_data_counter, year,month, day,quarter, weekday,hour)
    
            # =============================================================================
            #generating narratives (Distribution Narratives)
            # =============================================================================
    
            distribution_narratives={}
            DistributionNarrative=getDistributionNarratives(selected_data_counter_EncodeDateparts,pluralentity)
    
            distribution_narratives['Level1_Distribution_Narratives']=DistributionNarrative.to_dict('records')
            #multiple Distribution Narratives (using Correlation)
            utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            correlation_narratives=getNarrativebyCorrelationbetweenColumns(selected_data_counter_EncodeDateparts,pluralentity)
                #correlation_narratives.to_excel('correlation_narratives.xlsx')
            if correlation_narratives  is not None:
                distribution_narratives['Level2_Distribution_Narratives']=correlation_narratives.to_dict('records')
            else:
            
                distribution_narratives['Level2_Distribution_Narratives']=TextForNoLevel2DN()
            utils.updQdb(correlationId, 'P', '90', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            volumetric_narratives['distribution_narratives'] =distribution_narratives
            
            if EncryptionFlag:
                volumetric_narratives = EncryptData.EncryptIt(json.dumps(volumetric_narratives))

            
            #upating volumetric inferences to inferenceresults collection
            utils.update_inferences(correlationId, InferenceConfigId, InferenceConfigType, volumetric_narratives, userId)
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=False,requestId=requestId)
            utils.logger(logger, correlationId, 'INFO', 'GenerateVolumetric Completed',str(requestId))



'''
    correlationId = "6c57d67d-c3f1-4b32-9bbc-179786d961fe"   # '08062021-storydata' #'04062021-ioincidentdata'
    requestId = '563ba898-f26c-4ac6-9b1e-6ccd9afc291e'
    InferenceConfigId = "92a3207a-5106-437c-a8e2-3cac00abd68e"
    pageInfo = 'GetFeatures'
    userId = 'praveen kumar'
    
'''     
                
def getFeaturesPageInfo(correlationId,requestId,InferenceConfigId,pageInfo,userId,EncryptionFlag,logger):
            """
            #these details will get from paramargs
            
            date_for_target =   'Reported Date'
            target = 'SLA Resolution'#'Story points'#'User Story Readiness State (Ready/ Partially Ready/Not Ready)' #'Story points'
            filterdict={}
            
            """
            #-------------------------------------------------------------------------
            utils.logger(logger, correlationId, 'INFO', 'GetFeatures Invoked',str(requestId))
            utils.updQdb(correlationId, 'P', '5', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            target,date_for_target=utils.getMetricAndDateFromDB(correlationId,requestId)
            
            columnsfromDB=utils.getDimensionListFromDB(correlationId)  #this columnsfromDB includes both imp columns +suggested dims columns
            filterdict=utils.getFilterValuesFromDB(correlationId,target,date_for_target)

            utils.updQdb(correlationId, 'P', '20', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
            # data1 = utils.data_from_chunks(corid=correlationId,collection="IE_IngestData")
            # data1=data1.dropna(how='all')
            # threshold=int(len(data1)*0.02)
            # data_copy,unique_values_dict,null_value_columns=get_uniqueValues(data1)
            # databinns,updatedUniquedict,bincolumns,numericalcolumns=binningContinousColumn(data_copy,unique_values_dict,threshold)
            
            databinns = utils.data_from_chunks(corid=correlationId,collection="IE_PreprocessedData")
            bincolumns,numericalcolumns=utils.getBinAndNumericalColumnsFromDB(correlationId)
            
            dataformetric=filterInMeasureAnalysis(databinns,filterdict,numericalcolumns)
            #dataformetric.to_csv("dataInfeatures.csv")
            if dataformetric.shape[0]<2:
                raise Exception("Filter applied,Data have less than 2 records which is not sufficient to  generate Features and Feature combinations")
            
            if date_for_target!='':
                #print("date_for_target::",date_for_target)
                year=date_for_target+"_Year"  
                #quarter=date_for_target+"_Quarter" 
                month=date_for_target+"_Month" 
                day=date_for_target+'_Day'  
                weekday=date_for_target+'_Weekday'
                hour=date_for_target+'_Hour'
                
                
                dataformetric[date_for_target] =  dataformetric[date_for_target].dt.strftime('%d-%m-%Y %H:%M:%S')
                ##end##
                utils.updQdb(correlationId, 'P', '30', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
                datawithdates,updated_imp_columns=addingDateColumnInMetric(dataformetric, columnsfromDB, date_for_target)
                
                data_for_metric_analysis=encode_date_columns(datawithdates,year, month,day, weekday,hour)
            else:
                data_for_metric_analysis=dataformetric
                updated_imp_columns=columnsfromDB.copy()
                
    
            selected_data_for_metric=data_for_metric_analysis.copy()
            
            #excluding if there is any date columns
            datecol_list=identify_date_columns(selected_data_for_metric)
            selected_data_for_metric = selected_data_for_metric.select_dtypes(exclude=['datetime64[ns]', 'datetime64[ns, UTC]','datetime64[ns, tzlocal()]'])
            updated_imp_columns=list(set(updated_imp_columns)-set(datecol_list))

            utils.updQdb(correlationId, 'P', '40', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            #print("target :: "+target+" Date ::"+date_for_target)
            if str(selected_data_for_metric[target].dtype) in ['int64','float64','int32','float32']:
                continous_target=target
                if continous_target+"-bin" in updated_imp_columns:
                    updated_imp_columns.remove(continous_target+"-bin")
                #replacing null values for all columns (Since we copied data_with_ymdq)
                #all columns except target
                selectData_wo_dates_for_continousmetric=replace_null_values(selected_data_for_metric[updated_imp_columns]) #Make note that In this point updated_imp_columns not have target column                
                #for target column
                selectData_wo_dates_for_continousmetric[continous_target]=selected_data_for_metric[continous_target].fillna(selected_data_for_metric[continous_target].mean())
                                
                utils.updQdb(correlationId, 'P', '50', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                #important dimensions by regression model(random forest)
                fiften_imp_dimns_by_regression,list_columns_regression=get_important_dim_by_regression(selectData_wo_dates_for_continousmetric,continous_target)
                suggestedfeatures = set(list_columns_regression) - set(fiften_imp_dimns_by_regression)
                suggestedfeatures = list(suggestedfeatures)
                #connected dimensions  using chi 2  cramers v test
                #approch2 cramers v test
                connected_dim_list_regression_cc=chi2_cramersV_test_for_conn_dims(selectData_wo_dates_for_continousmetric, fiften_imp_dimns_by_regression, list_columns_regression)
                utils.updQdb(correlationId, 'P', '70', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                connected_dim_list_regression_cc=connected_dim_list_regression_cc.replace({np.nan: None})
                df_combinations=connectedFeaturesConversionToStr(connected_dim_list_regression_cc)
            
                utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)

                utils.updateFeaturesAndCombinations(correlationId,requestId,target,date_for_target,'continous',fiften_imp_dimns_by_regression,df_combinations,suggestedfeatures,list_columns_regression,userId)
                
        
                # #considering all features and combinations as user selected
    
                #utils.saveConfigInDB(correlationId, InferenceConfigId, 'MeasureAnalysis', 'InferenceConfigName', userId, target,'continous',date_for_target,fiften_imp_dimns_by_regression,df_combinations)
               
           
            else:
                cat_target=target
                #############
                updated_imp_columns.append(cat_target)
                updated_imp_columns=list(set(updated_imp_columns))
                ###################
                
                #replacing null values for all columns (Since we copied data_with_ymdq)
                #all columns with target(since cat target is already present in updated_imp_columns)
                selectData_wo_dates_for_cat_metric=replace_null_values(selected_data_for_metric[updated_imp_columns]) 
                utils.updQdb(correlationId, 'P', '50', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                #selectData_wo_dates_for_cat_metric.to_excel('selectData_wo_dates_for_cat_metric.xlsx')
    
                #important dimensions by classifier
                fiften_imp_dimns_by_classifier,list_columns_classifier=get_important_dim_by_classifier(selectData_wo_dates_for_cat_metric, cat_target)
                suggestedfeatures = set(list_columns_classifier) - set(fiften_imp_dimns_by_classifier)
                suggestedfeatures = list(suggestedfeatures)
                #print('praveen')
                utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                #connected dimensions  using chi2 cramers v test
                connected_dim_list_classifier_cc=chi2_cramersV_test_for_conn_dims(selectData_wo_dates_for_cat_metric, fiften_imp_dimns_by_classifier, list_columns_classifier)
                utils.updQdb(correlationId, 'P', '70', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                connected_dim_list_classifier_cc=connected_dim_list_classifier_cc.replace({np.nan: None})
                df_combinations_cc=connectedFeaturesConversionToStr(connected_dim_list_classifier_cc)
                utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)

                utils.updateFeaturesAndCombinations(correlationId,requestId,target,date_for_target,'categorical',fiften_imp_dimns_by_classifier,df_combinations_cc,suggestedfeatures,list_columns_classifier,userId)

            
                #utils.saveConfigInDB(correlationId, InferenceConfigId, 'MeasureAnalysis', 'InferenceConfigName', userId, target,'categorical',date_for_target,fiften_imp_dimns_by_classifier,df_combinations_cc)

        
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=False,requestId=requestId)
            utils.logger(logger, correlationId, 'INFO', 'GetFeatures Completed',str(requestId))



'''
    correlationId ='1cf68f5e-f984-4e4f-861b-0f9fbde47a94' #'08062021-storydata' #'04062021-ioincidentdata'
    requestId = 'bd153ee9-ba92-44c1-bbb7-3593563818d2'
    InferenceConfigId = 'inferenceConfigId'
    InferenceConfigType='MeasureAnalysis'   
    pageInfo = 'GenerateNarratives'
    userId = 'praveen kumar'
'''     
    
def generateNarrativesPageInfo(correlationId,requestId,InferenceConfigId,pageInfo,userId,EncryptionFlag,logger):
            utils.logger(logger, correlationId, 'INFO', 'GenerateNarratives Invoked',str(requestId))

            dbconn, dbcollection = utils.open_dbconn('IE_RequestQueue')
            args = dbcollection.find_one({"CorrelationId": correlationId, "RequestId": requestId})
            InferenceConfigType = args['InferenceConfigType']
            entity=utils.getEntityFromDB(correlationId)
            
            if entity == "":
                entity='entity data'
            pluralentity=getPluralForEntity(entity.lower())

            utils.updQdb(correlationId, 'P', '5', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            targetfornarrative,date,imp_features,connected_dims_dict,filterdict=utils.getFeaturesAndCombinations(correlationId,InferenceConfigId,InferenceConfigType)
            connected_df=pd.DataFrame(connected_dims_dict)
            #print("target:: ",targetfornarrative,"datefortarget:: ",date)
            connected_dims=connectedFeaturesConversionToDF(connected_df)
            
            #filterdict=utils.getFilterValuesFromDB(correlationId,targetfornarrative,date)
        
            utils.updQdb(correlationId, 'P', '20', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            columnsfromDB=utils.getDimensionListFromDB(correlationId)
            
            
            # data2 = utils.data_from_chunks(corid=correlationId,collection="IE_IngestData")
            # data2=data2.dropna(how='all')
            # threshold=int(len(data2)*0.02)
            # data_copy,unique_values_dict,null_value_columns=get_uniqueValues(data2)
            # dataMain,updatedUniquedict,bincolumns,numericalcolumns=binningContinousColumn(data_copy,unique_values_dict,threshold)
            
            dataMain = utils.data_from_chunks(corid=correlationId,collection="IE_PreprocessedData")
            bincolumns,numericalcolumns=utils.getBinAndNumericalColumnsFromDB(correlationId)
            
            dataMain=filterInMeasureAnalysis(dataMain,filterdict,numericalcolumns)
            #dataMain.to_csv("dataInnarratives.csv")
            if dataMain.shape[0]<2:
                raise Exception("Filter applied,Data have less than 2 records which is not sufficient to  generate Inferences")
            ##formatingbinvalues
            dataMain=formatBinValues(dataMain,bincolumns)
            
            if  date !='':
                #print(date)
                year=date+"_Year"  
                #quarter=date_for_target+"_Quarter" 
                month=date+"_Month" 
                day=date+'_Day'  
                weekday=date+'_Weekday'
                hour=date+'_Hour'
                
                ##changes##
                #dataMain=dataMain.dropna(subset=[date])
                dataMain[date] =  dataMain[date].dt.strftime('%d-%m-%Y %H:%M:%S')
                ##end##
                datawithdates1,imp_dims=addingDateColumnInMetric(dataMain, columnsfromDB, date)
                dataforNarrative=encode_date_columns(datawithdates1,year, month,day, weekday,hour)

            else:
                dataforNarrative=dataMain
                imp_dims=columnsfromDB.copy()
                
                
                
                  
        
            utils.updQdb(correlationId, 'P', '40', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            selected_data_for_narrative=dataforNarrative.copy()
            
            #excluding if there is any date columns
            datecol_list=identify_date_columns(selected_data_for_narrative)
            selected_data_for_narrative = selected_data_for_narrative.select_dtypes(exclude=['datetime64[ns]', 'datetime64[ns, UTC]','datetime64[ns, tzlocal()]'])
            imp_dims=list(set(imp_dims)-set(datecol_list))
            
            
            if str(selected_data_for_narrative[targetfornarrative].dtype) in ['int64','float64','int32','float32']:
                utils.updQdb(correlationId, 'P', '45', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                #replacing null values for all columns
                #all columns except target
                selectData_continousmetric_narratives=replace_null_values(selected_data_for_narrative[imp_dims]) #Make note that In this point imp_columns not have target column
                #for target column
                selectData_continousmetric_narratives[targetfornarrative]=selected_data_for_narrative[targetfornarrative].fillna(selected_data_for_narrative[targetfornarrative].mean())
                
                utils.updQdb(correlationId, 'P', '50', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
                
                if imp_features is not None:
                    #one dimension narratives(level1)
                    imp_dimns_by_regression_narratives,imp_dimns_by_regression_outliers_narratives=get_imp_dimension_narratives_and_outlier_narratives(selectData_continousmetric_narratives,targetfornarrative,imp_features)
                    imp_feat_narratives=imp_dimns_by_regression_narratives.to_dict(orient='records') 
                    if imp_dimns_by_regression_outliers_narratives is not None:    
                        level1outliers=imp_dimns_by_regression_outliers_narratives.to_dict(orient='records')
                    else:
                        level1outliers=TextForNoLevel1ContinousOutliers(targetfornarrative)
                    #print('level1 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level1 completed',str(requestId))


                else:
                    imp_feat_narratives=None
                    level1outliers=TextForNoLevel1ContinousOutliers(targetfornarrative)
                    #print('level1 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level1 completed',str(requestId))
                    
                   
                utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                #narratives with one connected dimension(level2)
                #regression model
                if connected_dims is not None:
                    
                    one_connected_dim_list_regression_narratives,one_connected_dim_list_regression_outliers_narratives=get_one_connected_narratives_and_outlier_narratives(selectData_continousmetric_narratives,targetfornarrative,connected_dims)
                    one_connected_narratives=one_connected_dim_list_regression_narratives.to_dict(orient='records')
                    
                    if one_connected_dim_list_regression_outliers_narratives is not None:    
                        level2outliers=one_connected_dim_list_regression_outliers_narratives.to_dict(orient='records')
                    else:
                        level2outliers=TextForNoLevel2ContinousOutliers(targetfornarrative)
                
                    #print('level2 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level2 completed',str(requestId))
          
                    utils.updQdb(correlationId, 'P', '70', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
                #narratives with two connected dimension(level3)
                    #Random forest regression model
                    
                    connected_dims_df=connected_dims.dropna()
                    if connected_dims_df.shape[0]!=0:
                        two_connected_dim_list_regression_narratives,two_connected_dim_list_regression_outliers_narratives=get_two_connected_narratives_and_outlier_narratives(selectData_continousmetric_narratives,
                                                                                                                                                                           targetfornarrative,connected_dims_df)
                        two_connected_narratives=two_connected_dim_list_regression_narratives.to_dict(orient='records')
                        if two_connected_dim_list_regression_outliers_narratives is not None:    
                            level3outliers=two_connected_dim_list_regression_outliers_narratives.to_dict(orient='records')
                        else:
                            level3outliers=TextForNoLevel3ContinousOutliers(targetfornarrative)
                    else:
                        two_connected_narratives=TextForNoLevel3MeasureNarratives(targetfornarrative)
                        level3outliers=TextForNoLevel3ContinousOutliers(targetfornarrative)
                        
                    
                    
                    #print('level3 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level3 completed',str(requestId))
                else:
                    one_connected_narratives=TextForNoLevel2MeasureNarratives(targetfornarrative)
                    level2outliers=TextForNoLevel2ContinousOutliers(targetfornarrative)
                    #print('level2 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level2 completed',str(requestId))
                    two_connected_narratives=TextForNoLevel3MeasureNarratives(targetfornarrative)
                    level3outliers=TextForNoLevel3ContinousOutliers(targetfornarrative)
                    #print('level3 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level3 completed',str(requestId))


                    

                    
                   
                utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                metric_narratives={
                                'level1_narratives':imp_feat_narratives,
                                'level2_narratives':one_connected_narratives,
                                'level3_narratives':two_connected_narratives,
                                'level1_outliers_narratives':level1outliers,
                                'level2_outliers_narratives':level2outliers,
                                'level3_outliers_narratives':level3outliers
                                }
                
                #metric_analysis['narratives']=metric_narratives 
                
                utils.updQdb(correlationId, 'P', '90', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
            
            else:
                utils.updQdb(correlationId, 'P', '45', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
                #############
                imp_dims.append(targetfornarrative)
                imp_dims=list(set(imp_dims))
                ###################
                
                selectData_for_cat_metric_narrative=replace_null_values(selected_data_for_narrative[imp_dims])
                                
                #narratives with imp_dim s (level1)
                if imp_features is not None:
                    imp_dim_list_classifier_narratives=get_imp_dim_narratives_with_cat_target(selectData_for_cat_metric_narrative, targetfornarrative, imp_features,pluralentity)
                    #imp_dim_list_classifier_narratives,imp_dim_list_classifier_outliers_narratives=get_imp_dim_narratives_with_cat_target_outlier_narratives(selectData_for_cat_metric_narrative, targetfornarrative, imp_features)
                    imp_feat_narratives=imp_dim_list_classifier_narratives.to_dict(orient='records') 

                    # if imp_dim_list_classifier_outliers_narratives is not None:    
                    #     level1outliers=imp_dim_list_classifier_outliers_narratives.to_dict(orient='records')
                        
                    # else:
                    #     level1outliers=None
                    
                    #print('level1 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level1 completed',str(requestId))

                else:
                    imp_feat_narratives=None
                    #level1outliers=None
                    #print('level1 completed')
                    utils.logger(logger, correlationId, 'INFO', 'level1 completed',str(requestId))
                    
                       
                utils.updQdb(correlationId, 'P', '50', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                #narratives with one connected dimension(level 2)
                #classification model
                    
                if connected_dims is not None:
                    
                    # one_connected_dim_list_classifier_narratives,one_connected_dim_list_classifier_outliers_narratives=get_one_connected_narratives_with_cat_target_outlier_narratives(selectData_for_cat_metric_narrative, targetfornarrative, connected_dims)
                    # one_connected_narratives=one_connected_dim_list_classifier_narratives.to_dict(orient='records')
                    # if one_connected_dim_list_classifier_outliers_narratives is not None:    
                    #     level2outliers=one_connected_dim_list_classifier_outliers_narratives.to_dict(orient='records')
                    # else:
                    #     level2outliers=None
                   
                    # print('level2 completed')
                    
                    # utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
                    # #   narratives with two connected dimension(level 3)
                    # #classification model
                    # two_connected_dim_list_classifier_narratives,two_connected_dim_list_classifier_outlier_narratives=get_two_connected_narratives_with_cat_target_outlier_narratives(selectData_for_cat_metric_narrative, targetfornarrative, connected_dims)
                    # two_connected_narratives=two_connected_dim_list_classifier_narratives.to_dict(orient='records')
                    # utils.updQdb(correlationId, 'P', '70', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
                    # if two_connected_dim_list_classifier_outlier_narratives is not None:    
                    #     level3outliers=two_connected_dim_list_classifier_outlier_narratives.to_dict(orient='records')
                    # else:
                    #     level3outliers=None
                        
                    # utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                    # print('level3 completed')
                    
                    utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
    
                    one_connected_dim_list_classifier_narratives,two_connected_dim_list_classifier_narratives=get_connected_narratives_with_cat_target(selectData_for_cat_metric_narrative, targetfornarrative, connected_dims,pluralentity)
                    if one_connected_dim_list_classifier_narratives.shape[0]==0:
                        one_connected_narratives=TextForNoLevel2MeasureNarratives(targetfornarrative)
                    else:
                        one_connected_narratives=one_connected_dim_list_classifier_narratives.to_dict(orient='records')
                    utils.logger(logger, correlationId, 'INFO', ' Level2 completed',str(requestId))
                    if two_connected_dim_list_classifier_narratives.shape[0]==0:
                        two_connected_narratives=TextForNoLevel3MeasureNarratives(targetfornarrative)
                    else:
                        two_connected_narratives=two_connected_dim_list_classifier_narratives.to_dict(orient='records')
                    utils.logger(logger, correlationId, 'INFO', ' Level3 completed',str(requestId))
                    utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
                    
                    
                
    
                else:
                    one_connected_narratives=TextForNoLevel2MeasureNarratives(targetfornarrative)
                    #level2outliers=None
                    #print('level2 completed')
                    utils.logger(logger, correlationId, 'INFO', ' Level2 completed',str(requestId))

                    utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
    
                    two_connected_narratives=TextForNoLevel3MeasureNarratives(targetfornarrative)
                    #level3outliers=None
                    #print('level3 completed')
                    utils.logger(logger, correlationId, 'INFO', ' Level3 completed',str(requestId))

                    utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
        
                level1outliers=TextForNoLevel1CategoryOutliers(targetfornarrative)
                level2outliers= TextForNoLevel2CategoryOutliers(targetfornarrative)
                level3outliers=TextForNoLevel3CategoryOutliers(targetfornarrative)
    
                metric_narratives={
                                'level1_narratives':imp_feat_narratives,
                                'level2_narratives':one_connected_narratives,
                                'level3_narratives':two_connected_narratives,
                                'level1_outliers_narratives':level1outliers,
                                'level2_outliers_narratives':level2outliers,
                                'level3_outliers_narratives':level3outliers
                                }
                utils.updQdb(correlationId, 'P', '90', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental = False,requestId=requestId)
            
            if EncryptionFlag:
                metric_narratives = EncryptData.EncryptIt(json.dumps(metric_narratives))
    
            utils.update_inferences(correlationId, InferenceConfigId, InferenceConfigType, metric_narratives, userId)
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=False,requestId=requestId)
            utils.logger(logger, correlationId, 'INFO', 'GenerateNarratives Completed',str(requestId))    
    