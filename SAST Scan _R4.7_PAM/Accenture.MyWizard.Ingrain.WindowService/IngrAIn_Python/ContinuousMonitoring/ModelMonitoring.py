import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
import numpy as np
from SSAIutils import EncryptData #En............................
import base64
import json
import datetime
import uuid
import time
from SSAIutils import utils
from Prescriptive_Analysis import new_features 
from ContinuousMonitoring import monitoring_utils as mutils
import datetime

def ModelingMetrics(CorrelationId,RequestId,pageInfo,userId,deployed_accuracy,model_name):
    EnDeRequired = utils.getEncryptionFlag(CorrelationId)
    
    dbconntar,dbcollectiontar = utils.open_dbconn("DE_DataCleanup")
    data_json_tar             = dbcollectiontar.find({"CorrelationId" :CorrelationId})    
    dbconntar.close()
    target_info     = data_json_tar[0]
    target_type     = target_info["Target_ProblemType"]
    
    
    dbconn,dbcollection = utils.open_dbconn("DataCleanUP_FilteredData")
    data_json           = dbcollection.find({"CorrelationId" :CorrelationId})    
    dbconn.close()
    k = list(data_json)
    input_data_unique = k[0].get("ColumnUniqueValues")
    if EnDeRequired :
        t = base64.b64decode(input_data_unique)     #DE55......................................
        input_data_unique = json.loads(EncryptData.DescryptIt(t))
    tar_var           = k[0].get('target_variable')
    input_data_types  = k[0].get("types")
    featureParams     = utils.getFeatureSelectionVariable(CorrelationId)
    input_columns     = featureParams["selectedFeatures"]
    
    dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
    data_json           = dbcollection.find({"CorrelationId" :CorrelationId})    
    dbconn.close()
    k = list(data_json)
    uid_list = k[0].get("delta_uids")
    uid = k[0].get("TargetUniqueIdentifier")
    
    if tar_var in input_columns:
        input_columns.remove(tar_var)
        
    for i in input_data_unique:
        if "" in input_data_unique[i]:
            input_data_unique[i].remove("")
    
    input_data         = pd.DataFrame()
    input_data         = new_features.get_OGData(CorrelationId)
    main_cols = input_columns+[tar_var]
    
    dbproconn,dbprocollection = utils.open_dbconn("DE_AddNewFeature")
    data_json                 = dbprocollection.find({"CorrelationId" :CorrelationId})
    try: 
        features_created          = data_json[0].get("Features_Created")
    except Exception:
        features_created = []
        
    if (len(features_created)>0):
        nf_list = new_features.new_features_data(features_created,input_data)
        for i in nf_list:
            input_data[i]=nf_list[i]
            if(i not in input_columns) and(i!=tar_var):
                input_columns.append(i)
                
    temp_type = {}
    for i in input_columns:
          temp_type.update({i:input_data_types[i]})    
    input_data_types = temp_type
    
    input_data            = input_data[main_cols]
    input_data            = input_data.astype(input_data_types)
    
    dbconnfimp,dbcollectionfimp = utils.open_dbconn("ME_FeatureSelection")
    data_json_fimp              = dbcollectionfimp.find({"CorrelationId" :CorrelationId})    
    dbconn.close()

    fimp    = data_json_fimp[0]["FeatureImportance"]
    fimp_df = pd.DataFrame(fimp)
    if tar_var in list(fimp_df.columns):
        fimp_df = fimp_df.drop(labels=tar_var,axis=1)
    fimp_series = pd.Series(fimp_df.iloc[1])
    fimp_series = fimp_series[input_columns]
    if len(fimp_series)>3:
        top_inf = fimp_series.sort_values()[-3:]
        top_inf = list(top_inf.index)
    else:
        top_inf = fimp_series.sort_values()
        top_inf = list(top_inf.index)

    date_object = datetime.datetime.now()
    date_string = date_object.strftime("%Y-%m-%d %H:%M:%S")
    dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
    data_json = dbcollection.find({"CorrelationId":CorrelationId})
    k=data_json[0]
    clientuid= k.get("ClientUId")
    dcuid = k.get("DeliveryConstructUID")
    created_date = k.get("CreatedOn")
    modified_date = date_string
    deployed_date =k.get("ModifiedOn")
    deployed_date = pd.to_datetime(deployed_date).tz_localize(None)
    
    try:
        dbconn,dbcollection = utils.open_dbconn("TrainedModelHistory")
        data_json = dbcollection.find({"CorrelationId":CorrelationId})
        k=data_json[0]
        Id1 = k["_id"]
        LastRequestId = k["LastRequestId"]
        d = {"LastRequestId":LastRequestId}
        dbcollection.update_one({"_id":Id1},{"$set":d})
        dbconn.close()
    except:
        LastRequestId = RequestId
        dbconn,dbcollection = utils.open_dbconn("TrainedModelHistory")
        Id1 = str(uuid.uuid4())   
        dbcollection.insert({
                             "_id" : Id1,
                             "CorrelationId":CorrelationId,
                             "LastRequestId":LastRequestId,
                             "ClientUId": clientuid,
                             "DeliveryConstructUID":dcuid,
                             "CreatedOn": created_date,
                             "CreatedByUser":userId,
                             "ModifiedOn":modified_date,
                             "ModifiedByUser":userId
                             })
    #######################################################################
    utils.updQdb(CorrelationId,'P','30',pageInfo,userId,UniId=RequestId)
    #######################################################################
    
    base_data,prod_data = mutils.prepare_prod_data(CorrelationId,deployed_date,input_data,uid,uid_list)
    if target_type=="1":
        actual_prod_target = prod_data[tar_var].astype("float32")
        actual_train_target = base_data[tar_var].astype("float32")
    else:
        actual_prod_target = prod_data[tar_var]
        actual_train_target = base_data[tar_var]

    input_data.drop([uid],axis=1,inplace=True)
    final_data = mutils.predict_target(prod_data,CorrelationId,input_columns,input_data_types,tar_var,RequestId,pageInfo,model_name)
    pred_target = final_data[tar_var]
    if uid in top_inf:
        top_inf.remove(uid)    
    today_date = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    monitoring_metrics = mutils.monitoring_parameters(prod_data,base_data,actual_prod_target,actual_train_target,pred_target,tar_var,target_type,top_inf,deployed_accuracy,input_columns,input_data_types)
    
    mutils.prepare_update_monitoring_graphdata(CorrelationId,LastRequestId,RequestId,monitoring_metrics,userId)
    
    #######################################################################
    utils.updQdb(CorrelationId,'P','60',pageInfo,userId,UniId=RequestId)
    #######################################################################
    
    if monitoring_metrics["ModelHealth"]=="Unhealthy":
        deployed_date = today_date
        dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        data_json= dbcollection.find({"CorrelationId":CorrelationId})
        deployed_id = data_json[0]["_id"]
        health = {"ModelHealth":"Unhealthy"}
        dbcollection.update({"_id":deployed_id},{"$set":health})
        dbconn.close()
        
        dbconn,dbcollection = utils.open_dbconn("TrainedModelHistory")
        data_json = dbcollection.find({"CorrelationId":CorrelationId})
        k = data_json[0]
        Id1 = k["_id"]
        try:
            trained_model_history = k["TrainedModelHistory"]
            trained_model_history.append({
                    "Accuracy":{"CurrentValue":float(monitoring_metrics["Accuracy"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["Accuracy"]["ThresholdValue"])},
                    "InputDrift":{"CurrentValue":float(monitoring_metrics["InputDrift"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["InputDrift"]["ThresholdValue"])},
                    "TargetVariance":{"CurrentValue":float(monitoring_metrics["TargetVariance"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["TargetVariance"]["ThresholdValue"])},
                    "DataQuality":{"CurrentValue":float(monitoring_metrics["DataQuality"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["DataQuality"]["ThresholdValue"])},
                    "Date":str(today_date)                    
                    })
            model_history={
                    "LastDeployedDate": str(deployed_date),
                    "TrainedModelHistory":trained_model_history
                    }
            dbcollection.update({"_id":Id1},{"$set":model_history})
        except:
            model_params = [{
                    "Accuracy":{"CurrentValue":float(monitoring_metrics["Accuracy"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["Accuracy"]["ThresholdValue"])},
                    "InputDrift":{"CurrentValue":float(monitoring_metrics["InputDrift"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["InputDrift"]["ThresholdValue"])},
                    "TargetVariance":{"CurrentValue":float(monitoring_metrics["TargetVariance"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["TargetVariance"]["ThresholdValue"])},
                    "DataQuality":{"CurrentValue":float(monitoring_metrics["DataQuality"]["CurrentValue"]),"ThresholdValue":float(monitoring_metrics["DataQuality"]["ThresholdValue"])},
                    "Date":str(today_date)
                        }]
            model_history = {
                "LastDeployedDate":str(deployed_date),
                "TrainedModelHistory":model_params
                }
            dbcollection.update({"_id":Id1},{"$set":model_history})   
        dbconn.close()
    else:
        dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        data_json= dbcollection.find({"CorrelationId":CorrelationId})
        deployed_id = data_json[0]["_id"]
        health = {"ModelHealth":"Healthy"}
        utils.setRetrain(CorrelationId,False)
        dbcollection.update({"_id":deployed_id},{"$set":health})
        dbconn.close()
    dbconn,dbcollection = utils.open_dbconn("TrainedModelHistory")
    data_json= dbcollection.find({"CorrelationId":CorrelationId})
    id1 = data_json[0]["_id"]
    dbcollection.update({"_id":id1},{"$set":{"LastRequestId":RequestId}})
    dbconn.close()
    #######################################################################
    utils.updQdb(CorrelationId,'C','100',pageInfo,userId,UniId=RequestId)
    #######################################################################