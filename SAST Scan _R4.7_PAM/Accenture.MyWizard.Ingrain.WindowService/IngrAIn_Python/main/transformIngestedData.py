# -*- coding: utf-8 -*-
"""
Created on Sat Jan 23 17:27:49 2021

@author: shrayani.mondal
"""
#Check-in for correct build
import platform
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'

import configparser,os
mainPath =os.getcwd()+work_dir
import sys
sys.path.insert(0,mainPath)
import file_encryptor
config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)

from SSAIutils import utils
import pandas as pd
from pandas import Timestamp
from datetime import datetime


#pageInfo = "TransformIngestedData"
#correlationId = "92fc2def-d292-4c8a-b2a2-635df1525ec1"
#cascadeCorrelationID = "62fc2ece-d292-4c8a-b2a2-635df1525ec1"
def main(correlationId,pageInfo,userId,cascadeCorrelationID):    
    try:
        utils.updQdb(correlationId,'P','10',pageInfo,userId)
        
        #original data for Model1
        currentResultDf = utils.data_from_chunks(corid=cascadeCorrelationID, collection="PS_IngestedData")
        
        #get the UniqueIdentifier for Model1
        dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId": cascadeCorrelationID}))
        dbconn.close()
        currentUniqueId = data_json[0]["TargetUniqueIdentifier"]  
        
        #get the predicted data for Model1
        dbconn, dbcollection = utils.open_dbconn("SSAI_PredictedData")
        data_json = list(dbcollection.find({"CorrelationId": cascadeCorrelationID}))
        dbconn.close()
        cascadeResult = data_json[0]["PredictedResult"]
        cascadeResultDf = pd.json_normalize(cascadeResult, sep = '_')
        
        utils.updQdb(correlationId,'P','20',pageInfo,userId)
        
        #Rename prediction probablity of Met as "PredictedOutcome"
        cascadeResultDf.rename(columns={"TargetScore_Met":"PredictedOutcome"}, inplace = True)
        
        #drop these columns ["TargetScore_Missed","Target"]
        drop_columns = ["TargetScore_Missed","Target"]
        cascadeResultDf.drop(drop_columns, axis=1, inplace = True, errors="ignore")
        df_merge = pd.merge(currentResultDf, cascadeResultDf, left_on = currentUniqueId, right_on = "ID", suffixes=(False, False))
        df_merge.drop(["ID"], axis=1, inplace = True, errors="ignore")
        
        utils.updQdb(correlationId,'P','30',pageInfo,userId)
        
        #derive ["Planned Hours","Requested Hours"] 
        date_cols = ["PlannedEndDateTime","PlannedStartDateTime","RequestedEndDateTime",
                     "RequestedStartDateTime"]
        df_merge[date_cols] = df_merge[date_cols].apply(pd.to_datetime)
        df_merge["Planned Hours"] = round((df_merge["PlannedEndDateTime"]-df_merge["PlannedStartDateTime"]).dt.seconds/3600,1)
        df_merge["Requested Hours"] = round((df_merge["RequestedEndDateTime"]-df_merge["RequestedStartDateTime"]).dt.seconds/3600,1)
        
        #create a field Sort Order, Scaled Sort Order, and PredOut 
        df_merge["Sort Order"] = df_merge.groupby(["Release Name","PrimaryImpactedApplication"])["PredictedOutcome"].rank(ascending=False, method="first")
        
        df_merge["Scaled Sort Order"] = df_merge["PredictedOutcome"]*df_merge["Sort Order"]
        
        df_merge["Numerator"] = df_merge.groupby(["Release Name","PrimaryImpactedApplication"])["Scaled Sort Order"].transform("sum")
        df_merge["Denominator"] = df_merge.groupby(["Release Name","PrimaryImpactedApplication"])["Sort Order"].transform("sum")
        df_merge["PredOut"] = round(df_merge["Numerator"]/df_merge["Denominator"],2)
        df_merge.drop(columns=["Numerator","Denominator"],inplace=True, errors="ignore")
        
        utils.updQdb(correlationId,'P','40',pageInfo,userId)
        
        #create the derived dataframe
        derived_data = df_merge.copy()
        
        #create ["Average Planned Hours", "Average Requested Hours"] groupby "Release Name"
        derived_data["Average Planned Hours"] = round(derived_data.groupby(["Release Name"])["Planned Hours"].transform("mean"),2)
        derived_data["Average Requested Hours"] = round(derived_data.groupby(["Release Name"])["Requested Hours"].transform("mean"),2)
        
        #drop unnecessary columns
        derived_data.drop(columns=["ChangeStartDate", "Category",
               "SubCategory", "AssignmentGroup","SupportCompany","Risk", "PlannedEndDateTime",
               "PlannedStartDateTime","RequestedEndDateTime",
               "RequestedStartDateTime","AssetID", "AssetStatus",
               "AssetType", "Week","SNChangeNumber"],inplace=True,errors='ignore')
        
        utils.updQdb(correlationId,'P','50',pageInfo,userId)
        
        #create PredOut_+PrimaryImpactedApplication for each PrimaryImpactedApplication
        #create ChangeType+PrimaryImpactedApplication for each (ChangeType,PrimaryImpactedApplication) pair
        #print("derivedData",derived_data.shape,derived_data.head())
        file1 = open("myfile.txt","w+")
        file1.write(str(derived_data.shape))
        file1.write(str(derived_data.head()))
        file1.close()
        derived_data.to_csv("fmData.csv")
        for i,j in derived_data[['PrimaryImpactedApplication','Release Name']].values:
            if str('PredOut_'+i) not in derived_data.columns:
                derived_data['PredOut_'+i] = 0
            derived_data.loc[derived_data['Release Name']==j,'PredOut_'+i] = derived_data[(derived_data['PrimaryImpactedApplication']==i) & (derived_data['Release Name']==j)]['PredOut'].unique().copy()[0]
            
            change_type = derived_data[(derived_data['PrimaryImpactedApplication']==i) & (derived_data['Release Name']==j)]['ChangeType'].value_counts()
            for x in change_type.index:
                derived_data.loc[derived_data['Release Name']==j,x+'_'+i] = change_type[x]
        
        utils.updQdb(correlationId,'P','60',pageInfo,userId)
        
        #calculate "Release Success Probability" for each release
        dict1 = derived_data.loc[derived_data["ChangeOutcome"]=="Met","Release Name"].value_counts().reset_index().rename(columns={"Release Name":"Numerator"})
        data_final = pd.merge(derived_data,dict1,left_on=["Release Name"],right_on=["index"],how="left")
        data_final.drop(columns=["index"],inplace=True,errors="ignore")
        data_final["Numerator"].fillna(0,inplace=True)
        data_final["Denominator"] = data_final.groupby(["Release Name"])["Numerator"].transform("count")
        data_final["Release Success Probability"] = data_final["Numerator"]/data_final["Denominator"]
        data_final.drop(columns=["Numerator","Denominator"],inplace=True,errors="ignore")
        
        #calculate "Release End Date" for each release
        data_final["Release End Date"] = df_merge.groupby(["Release Name"])["PlannedEndDateTime"].transform("min")
        
        #drop unnecessary columns
        data_final.drop(columns=["ChangeType", "PrimaryImpactedApplication",
               "Planned Hours", "Requested Hours", "ChangeOutcome", "PredictedOutcome", "Sort Order",
               "Scaled Sort Order","PredOut"], inplace=True, errors="ignore")
        
        utils.updQdb(correlationId,'P','70',pageInfo,userId)
            
        #finally add ["Total Tickets"] for each release and drop duplicates
        data_final["Total Tickets"] = derived_data.groupby(["Release Name"])["Release Name"].transform("count")
        data_final.drop_duplicates(inplace = True)
        
        utils.updQdb(correlationId,'P','80',pageInfo,userId)
        
        #insert the transformed data into PS_IngestedData 
        dbconn, dbcollection = utils.open_dbconn("PS_IngestedData")
        data_json = list(dbcollection.find({"CorrelationId": cascadeCorrelationID}))
        sourceDetails = data_json[0]["SourceDetails"]
        dbconn.close()
        
        columns = list(data_final.columns)
        chunks, filesize = utils.file_split(data_final)
        
        utils.save_data_chunks(chunks, "PS_IngestedData", correlationId, pageInfo, userId,
                               columns, Incremental = False, requestId = None,
                               sourceDetails = sourceDetails, colunivals = None,
                               timeseries = None, datapre = False, lastDateDict = None,
                               previousLastDate = None)
    
        #update PS_UsecaseDefinition with details of transformed data
        utils.UsecaseDefinitionDetails(data_final,correlationId,userId)
        
        utils.updQdb(correlationId,'C','100',pageInfo,userId)
    except Exception as e:
        utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId)
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
    else:
        utils.logger(logger, correlationId, 'INFO',
                     ( " FM data transformation completed successfully"),str(requestId))


try:
    correlationId = sys.argv[1]
    requestId = sys.argv[2]
    pageInfo = sys.argv[3]
    userId = sys.argv[4]
    cascadeCorrelationID = sys.argv[5]
    logger = utils.logger('Get',correlationId)
    utils.logger(logger,correlationId,'correlationId',correlationId,str(requestId))
    utils.updQdb(correlationId,'P','0',pageInfo,userId)
    utils.logger(logger, correlationId, 'INFO', ('Transformed Ingestdata started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    main(correlationId,pageInfo,userId,cascadeCorrelationID)
    utils.logger(logger, correlationId, 'INFO', ('Transformed Ingestdata completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
except Exception as e:
    logger = utils.logger('Get',correlationId)
    utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
    utils.updQdb(correlationId,'E','ERROR',pageInfo,userId)
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
else:
    utils.logger(logger,correlationId,'INFO','Task scheduled',str(requestId))
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()


