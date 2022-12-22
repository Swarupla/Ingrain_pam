# -*- coding: utf-8 -*-
"""
Created on Mon Feb  8 15:56:48 2021

@author: shrayani.mondal
"""

import platform
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

import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
import datetime
import json
import base64
from SSAIutils import utils
from SSAIutils import EncryptData
from main import publishModel_C
from pandas import Timestamp


def getMinMax(results):
    min_val = 0
    max_val = 0
    for key,value in results.items():
        for chunk_pred in eval(value):
            min_val = min(min_val, chunk_pred["predictedValue"])
            max_val = max(max_val, chunk_pred["predictedValue"])
    return min_val,max_val

def getFeatureValues(cascaded_corid, modelName, results, mapped_column = None):
    dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
    data_json = list(dbcollection.find({"CorrelationId":cascaded_corid,"UniqueId": uniqueId}))
    
    baseModelUniqueId = utils.getBaseModelUniqueId(cascaded_corid)
    
    test_data = []
    for i in range(len(data_json)):
        actual_data = str(data_json[i].get("ActualData"))
        CascadeEnDeRequired = utils.getEncryptionFlag(cascaded_corid)
        if CascadeEnDeRequired:
            t = base64.b64decode(actual_data)
            try:
                data = eval((EncryptData.DescryptIt(t)).encode('ascii',"ignore").decode('ascii'))
            except:
                data = json.loads((EncryptData.DescryptIt(t)).encode('ascii',"ignore").decode('ascii'))
        else:
            data = actual_data.strip().rstrip()
            data = data.encode('ascii',"ignore").decode('ascii')
            if isinstance(data,str):
                try:
                    data = eval(data)
                except SyntaxError:
                    data = eval(''.join([i if ord(i) < 128 else ' ' for i in data]))
                except:
                    data = json.loads(data)
        test_data = test_data + data

    test_data = pd.DataFrame(test_data)
    
    if mapped_column!=None:
        cascadedColumns = pd.DataFrame()
        for key,value in mapped_column.items():
            if cascadedColumns.empty:
                cascadedColumns = pd.DataFrame(mapped_column[key])
            else:
                cascadedColumns = cascadedColumns.append(pd.DataFrame(mapped_column[key]), ignore_index=True)
        
        cascadedColumns[baseModelUniqueId] = test_data[baseModelUniqueId]
        
        common_cols = list(set(cascadedColumns.columns).intersection(set(test_data.columns)))
        if len(common_cols)==0:
            test_data = pd.merge(test_data, cascadedColumns, on = baseModelUniqueId, suffixes=(False, False))
        else:
            for col in common_cols:
                if col!=baseModelUniqueId:
                    test_data.drop(col,axis=1,inplace=True)
            test_data = pd.merge(test_data, cascadedColumns, on = baseModelUniqueId, suffixes=(False, False))
        
                
    columnNames = list(eval(results["Chunk 0"])[0]["FeatureWeights"].keys())
    #Remove Cluster0, CLuster1 ...
    #clusterCols = [i for i in columnNames if i.startswith('Cluster')]
    clusterCols = [str("Cluster"+str(i)) for i in range(0,16)]
    columnNames = [col for col in columnNames if col not in clusterCols]
    
    test_data = test_data[columnNames+[baseModelUniqueId]]
    
    for col in columnNames:
        test_data.rename(columns = {col : str(modelName)+"_"+str(col)}, inplace=True)
    
    #if mapped_column!=None:
    #    test_data = pd.merge(test_data, cascadedColumns, on = baseModelUniqueId, suffixes=(False, False))

    return test_data,baseModelUniqueId

def addFeatureValues(featureValues,baseModelUniqueId,visualization):
    featureValues = featureValues.to_dict(orient="records")
    temp_dict = {}
    
    for item in featureValues:
        poppedId = item.pop(baseModelUniqueId)
        temp_dict[str(poppedId)] = item
    
    for key in temp_dict.keys():
        visualization["FeatureWeights"][str(key)]["FeatureValues"] = temp_dict[str(key)]
    
    return visualization

def initViz(dbMappings, lastDeployedModel, results, modelNames):
    
    visualization = {"PredictionProbability":[], "FeatureWeights":{}}
    id_name = dbMappings["Model1"]["UniqueMapping"]["Source"]
    
    clickable = {}
    for key, value in dbMappings.items():
        clickable["Model"+str(int(key[-1])+1)] = value["TargetMapping"]["Target"]
        
    for key,value in results.items():
        for chunk_pred in eval(value):
            visualization["FeatureWeights"]["DeployedTill"] = lastDeployedModel
            visualization["FeatureWeights"].update({str(chunk_pred[id_name]):{}})
            visualization["FeatureWeights"][str(chunk_pred[id_name])]["Model1"] = {}
            visualization["FeatureWeights"][str(chunk_pred[id_name])]["Model2"] = {}
            visualization["FeatureWeights"][str(chunk_pred[id_name])]["Model3"] = {}
            visualization["FeatureWeights"][str(chunk_pred[id_name])]["Model4"] = {}
            visualization["FeatureWeights"][str(chunk_pred[id_name])]["Model5"] = {}
            visualization["FeatureWeights"][str(chunk_pred[id_name])]["Clickable"] = clickable
            visualization["FeatureWeights"][str(chunk_pred[id_name])]["ModelName"]  = modelNames
            
    return visualization

def updateViz(visualization, results, modelNumber, lastDeployedModel, correlationId):
    
    id_name = utils.uniqueIdentifier(correlationId)
                
    for key,value in results.items():
        for chunk_pred in eval(value):
            #do not include clusters
            clusterCols =  [str("Cluster"+str(i)) for i in range(0,16)]
            visualization["FeatureWeights"][str(chunk_pred[id_name])][modelNumber] = {k:round(v,3) for k,v in chunk_pred["FeatureWeights"].items() if k not in clusterCols}
            #visualization["FeatureWeights"][str(chunk_pred[id_name])][modelNumber] = {k:round(v,3) for k,v in chunk_pred["FeatureWeights"].items() if not k.startswith("Cluster")}
            if modelNumber == lastDeployedModel:
                if "predictionProbability" in chunk_pred.keys():
                    categories = pd.DataFrame.from_dict(chunk_pred["predictionProbability"], orient='index', columns = ['value'])
                    categories.reset_index(inplace = True)
                    categories.rename(columns={"index": "name"}, inplace = True)
                    categories = categories.to_dict('records')
                    categories = [{'name': item['name'],'value': round(item['value'],3)} for item in categories]
                    visualization["PredictionProbability"].append({"Id":str(chunk_pred[id_name]), "IdName" : id_name, "TargetName" : chunk_pred["targetName"], "Categories" : categories})
                else:
                    min_val, max_val = getMinMax(results)
                    outcome = [{"value":chunk_pred["predictedValue"], "Min" :min_val, "Max" :max_val, "name" : ""}]
                    visualization["PredictionProbability"].append({"Id":str(chunk_pred[id_name]), "IdName" : id_name, "TargetName" : chunk_pred["targetName"], "Outcome":outcome})
    return visualization

def getCascadeType(correlationId):
    dbconn,dbcollection = utils.open_dbconn('SSAI_DeployedModels')
    data_json = list(dbcollection.find({"CorrelationId":correlationId}))
    dbconn.close()
    
    if data_json[0].get("IsCascadingButton"):
        cascadeType = "CustomCascade"
    else:
        cascadeType = "Cascade"
        
    return cascadeType

def getModelName(correlationId):
    dbconn,dbcollection = utils.open_dbconn('SSAI_DeployedModels')
    data_json = list(dbcollection.find({"CorrelationId":correlationId}))
    dbconn.close()
    
    return data_json[0].get("ModelName")
      
def getCascadedCols(results, dbMappings, correlationId):
    
    predictedCols = {}
    cascadeType = getCascadeType(correlationId)
    if cascadeType == "Cascade":
        
        cascadedTarget = dbMappings.get('TargetMapping').get('Target')
        
        for key,value in results.items():
            predicted_target=[]
            for getvalue in eval(value):
                predicted_target.append(getvalue.get('predictedValue'))
            predictedCols[key] = {cascadedTarget : predicted_target}
        
    elif cascadeType == "CustomCascade":
        
        cascadedTarget = dbMappings.get('TargetMapping').get('Target')
        cascadedProba1 = getModelName(correlationId) + "_Proba1"
        for key,value in results.items():
            predicted_target=[]
            predicted_proba1 = []
            for getvalue in eval(value):
                predicted_target.append(getvalue.get('predictedValue'))
                if "OthPred" in getvalue.keys():
                    predicted_proba1.append(max(getvalue["predictionProbability"].values()))
            predictedCols[key] = {cascadedTarget : predicted_target}
            if len(predicted_proba1)!=0:
                predictedCols[key][cascadedProba1] = predicted_proba1
                
    return predictedCols

def main(correlationId,uniqueId,pageInfo,userId):
    
    try:
        cascaded_corid = correlationId
        dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
        data_json = list(dbcollection.find({"CorrelationId":cascaded_corid}))
        
        dbconn,dbcollection = utils.open_dbconn('SSAI_CascadedModels')
        data_json = list(dbcollection.find({"CascadedId":cascaded_corid}))
        dbModelList = data_json[0].get('ModelList')
        dbMappings = data_json[0].get('Mappings')
        dbMappingData = data_json[0].get('MappingData')
        
        modelNames = {"Model1":"","Model2":"","Model3":"","Model4":"","Model5":""}
        for key,value in dbMappingData.items():
            modelNames[key] = getModelName(value["CorrelationId"])
        
        counter=0
        model_list = []
        for key,value in dbModelList.items():
            if isinstance(value,dict):
                model_list.append(key)
                counter = counter+1
            if counter == 0:
                raise Exception("Cascaded Model Values are Empty!")
        
        mappings_list=[]
        for key,value in dbMappings.items():
            
            if isinstance(value,dict):
                mappings_list.append(key)
        
        if len(set(model_list)-set(mappings_list))!=1:
            raise Exception("Mappings are not correct in Order. Kindly Check again")
        
        deployedFlag,lastDeployedModel = utils.checkIfLastModelDeployed(cascaded_corid)
        
        last_col = list(set(model_list)-set(mappings_list))
        
        if not deployedFlag:
            model_list = list(set(model_list)-set(last_col))
            last_col = [lastDeployedModel]
        
        if (len(dbModelList.keys()) == 1) or (lastDeployedModel == "Model1"):
            
            correlationId = dbModelList.get('Model1').get('CorrelationId')
            
            uniqueId = dbModelList.get('Model1').get('CorrelationId')
            results = publishModel_C.main_wrapper(correlationId,uniqueId,pageInfo,mapped_column=None,unique_identifer=None,counter=0,cascaded_corid=cascaded_corid)
            
            featureValues,baseModelUniqueId = getFeatureValues(cascaded_corid, "Model1", results)
            #initialize visualization as an empty dictionary
            visualization = initViz(dbMappings, lastDeployedModel, results, modelNames)
            
            #update visualization
            visualization = updateViz(visualization, results, "Model1", lastDeployedModel, correlationId)
            
            visualization = addFeatureValues(featureValues,baseModelUniqueId,visualization)
            
            #insert visualization
            dbconn,dbcollection = utils.open_dbconn('SSAI_CascadeVisualization')
            dbcollection.update_many({"CorrelationId": cascaded_corid,"UniqueId":uniqueId},
                                { "$set":{      
                                   "CreatedByUser"   : userId,
                                   "CreatedOn"       : str(datetime.datetime.now()),
                                   "Visualization"   : visualization
                                   }},upsert=True)
            dbconn.close()
            
            if len(results.keys())==1:
                dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
                data_json = list(dbcollection.find({"CorrelationId":cascaded_corid}))
                dbcollection.update_one({"CorrelationId":cascaded_corid,"UniqueId":uniqueId},{'$set':{         
                                        'PredictedData' : results["Chunk 0"],
                                        'Status' : "C",
                                        'Progress':"100"                                                 
                                       }})
            elif len(results.keys())>1:
                for i in range(len(results.keys())):
                    dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
                    data_json = list(dbcollection.find({"CorrelationId":cascaded_corid}))
                    dbcollection.update_one({"CorrelationId":cascaded_corid,"UniqueId":uniqueId,"Chunk_number":i},{'$set':{
                                            'PredictedData' : results["Chunk "+str(i)],
                                            'Status' : "C",
                                            'Progress':"100"                                                 
                                           }})
            
        elif len(dbModelList.keys())>1:
             predicted_arrays={}
             featureValues = pd.DataFrame()
             counter=0
             
             for x in model_list:  
                
                if x!=last_col[0]:
                    if not predicted_arrays:
                        correlationId = dbModelList.get(x).get('CorrelationId')    
                        results = publishModel_C.main_wrapper(correlationId,uniqueId,pageInfo,mapped_column=None,unique_identifer=None,counter=0,cascaded_corid=cascaded_corid)
                        predictedCols = getCascadedCols(results, dbMappings[x], correlationId)
                        previous_model = x
                        counter = counter+1
                        predicted_arrays.update({x:predictedCols})
                        
                        featureValues,_ = getFeatureValues(cascaded_corid, x, results)
                        #initialize visualization as an empty dictionary
                        visualization = initViz(dbMappings, lastDeployedModel, results, modelNames)
                        
                        #update visualization
                        visualization = updateViz(visualization, results, x, lastDeployedModel, correlationId)
                        
                    elif predicted_arrays:
                        correlationId = dbModelList.get(x).get('CorrelationId')
                        unique_identifer = dbMappings.get(previous_model)
                        counter = counter+1
                        mapped_column = predicted_arrays.get(previous_model)
                        results = publishModel_C.main_wrapper(correlationId,uniqueId,pageInfo,mapped_column=mapped_column,unique_identifer=unique_identifer,counter=counter,cascaded_corid=cascaded_corid)
                        predictedCols = getCascadedCols(results, dbMappings[x], correlationId)
                        previous_model = x
                        predicted_arrays.update({x:predictedCols})
                        
                        tempFeatureValues,baseModelUniqueId = getFeatureValues(cascaded_corid, x, results, mapped_column = mapped_column)
                        featureValues = pd.merge(featureValues, tempFeatureValues, on = baseModelUniqueId, suffixes=(False, False))
                        #update visualization
                        visualization = updateViz(visualization, results, x, lastDeployedModel, correlationId)
                 
                elif x==last_col[0]:
                    
                    correlationId = dbModelList.get(x).get('CorrelationId')
                    mapped_column = predicted_arrays.get(previous_model)
                    try:
                        unique_identifer = dbMappings.get(previous_model)
                    except:
                        unique_identifer = None
                    results = publishModel_C.main_wrapper(correlationId,uniqueId,pageInfo,mapped_column,unique_identifer=unique_identifer,counter=counter,cascaded_corid=cascaded_corid)
                    
                    tempFeatureValues,baseModelUniqueId = getFeatureValues(cascaded_corid, x, results, mapped_column = mapped_column)
                    featureValues = pd.merge(featureValues, tempFeatureValues, on = baseModelUniqueId, suffixes=(False, False))
                    featureValues.fillna('', inplace = True)

                    #update visualization
                    visualization = updateViz(visualization, results, x, lastDeployedModel, correlationId)
                    
                    visualization = addFeatureValues(featureValues,baseModelUniqueId,visualization)
                    #insert visualization
                    dbconn,dbcollection = utils.open_dbconn('SSAI_CascadeVisualization')
                    dbcollection.update_many({"CorrelationId": cascaded_corid,"UniqueId":uniqueId},
                                        { "$set":{      
                                           "CreatedByUser"   : userId,
                                           "CreatedOn"       : str(datetime.datetime.now()),
                                           "Visualization"   : visualization
                                           }},upsert=True)
                    dbconn.close()
                    
                    if len(results.keys())==1:
                        dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
                        data_json = list(dbcollection.find({"CorrelationId":cascaded_corid}))
                        dbcollection.update_one({"CorrelationId":cascaded_corid,"UniqueId":uniqueId},{'$set':{         
                                                'PredictedData' : results["Chunk 0"],
                                                'Status' : "C",
                                                'Progress':"100"                                                 
                                               }})
                    elif len(results.keys())>1:
                        for i in range(len(results.keys())):
                            dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
                            data_json = list(dbcollection.find({"CorrelationId":cascaded_corid}))
                            dbcollection.update_one({"CorrelationId":cascaded_corid,"UniqueId":uniqueId,"Chunk_number":i},{'$set':{
                                                    'PredictedData' : results["Chunk "+str(i)],
                                                    'Status' : "C",
                                                    'Progress':"100"                                                 
                                                   }})
                     
        
    except Exception as e:
        raise Exception(e.args[0])
try:
    
    correlationId = sys.argv[1]
    uniqueId = sys.argv[2]
    pageInfo = sys.argv[3]
    userId = sys.argv[4]
    logger = utils.logger('Get',correlationId)
    utils.updQdb(correlationId,'P','0',pageInfo,userId,UniId = uniqueId)
    utils.logger(logger,correlationId,'INFO',
                 (' Cascading Starts: Arguments '+str(sys.argv)+
                  ' CorrelationID :'+str(correlationId)+
                  ' UniqueID :'+str(uniqueId)+
                  ' pageInfo :'+pageInfo+
                  ' UserId :'+userId),str(uniqueId))
    utils.logger(logger, correlationId, 'INFO', ('Metric pull started at '  + str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
    main(correlationId,uniqueId,pageInfo,userId)
    utils.logger(logger, correlationId, 'INFO', ('Metric pull completed at '  + str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
    utils.updQdb(correlationId,'C','100',pageInfo,userId,UniId = uniqueId)
except Exception as e:
    correlationId='Error_corrid'
    uniqueId='Unique_ErrorId'
    logger = utils.logger('Get',correlationId)
    utils.logger(logger,correlationId,'ERROR','Trace',str(uniqueId))
    utils.updateErrorInTable(e.args[0], correlationId, uniqueId)
    utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,userId,UniId = uniqueId)
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    
else:
    utils.logger(logger,correlationId,'INFO','Task scheduled',str(uniqueId))
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    
             
             


    
