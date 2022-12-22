import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
from datapreprocessing import DataEncoding
from datapreprocessing import Data_Modification
import pandas as pd 
import lime  
import lime.lime_tabular
import datetime
import uuid
import time
import re 
import collections
import numpy as np
import math
import json
from sklearn.preprocessing import LabelEncoder,StandardScaler
import itertools
from Prescriptive_Analysis import pres_preprocessing
from Prescriptive_Analysis import filters 
from Prescriptive_Analysis import approx  
from Prescriptive_Analysis import new_features 
from SSAIutils import EncryptData #En............................
import base64
import json

#correlationId="c8e3a240-f92c-4cfb-8f4c-51e50b993cc2"
#requestId="ec0f3f92-f09c-44b4-a707-6e360c321cfd"
#WFId="e8718df6-eeef-4aa7-8922-b18c54fb6a06"
#pageinfo = "PrescriptiveAnalytics"
#userId="mywizardsystemdataadmin@mwphoenix.onmicrosoft.com"
#desired_value = "55.24"

def main(correlationId,WFId,requestId,pageinfo,userId,desired_value):
    
    EnDeRequired = utils.getEncryptionFlag(correlationId)
    dbconn,dbcollection = utils.open_dbconn("DataCleanUP_FilteredData")
    data_json           = dbcollection.find({"CorrelationId" :correlationId})    
    dbconn.close()
    k                 = list(data_json)
    input_data_unique = k[0].get("ColumnUniqueValues")
    if EnDeRequired :
        t = base64.b64decode(input_data_unique)     #DE55......................................
        input_data_unique = json.loads(EncryptData.DescryptIt(t))
    tar_var           = k[0].get('target_variable')
    input_data_types  = k[0].get("types")
    featureParams     = utils.getFeatureSelectionVariable(correlationId)
    input_columns     = featureParams["selectedFeatures"]
    
    if tar_var in input_columns:
        input_columns.remove(tar_var)
        
    for i in input_data_unique:
        if "" in input_data_unique[i]:
            input_data_unique[i].remove("")
    

    raw_input_data         = pd.DataFrame()
    raw_input_data_desired = pd.DataFrame()
    raw_input_data         = new_features.get_OGData(correlationId)
    main_cols = input_columns+[tar_var]
    
    dbproconn,dbprocollection = utils.open_dbconn("DE_AddNewFeature")
    data_json                 = dbprocollection.find({"CorrelationId" :correlationId})
    try: 
        features_created          = data_json[0].get("Features_Created")
    except Exception:
        features_created = [] 

    if (len(features_created)>0):
        nf_list = new_features.new_features_data(features_created,raw_input_data)
        for i in nf_list:
            raw_input_data[i]=nf_list[i]
            if(i not in input_columns) and(i!=tar_var):
                input_columns.append(i)
                
    temp_type = {}
    for i in input_columns:
          temp_type.update({i:input_data_types[i]})    
    input_data_types = temp_type
        
    raw_input_data            = raw_input_data[main_cols]
    raw_input_data            = raw_input_data.astype(input_data_types)
    dbconntar,dbcollectiontar = utils.open_dbconn("DE_DataCleanup")
    data_json_tar             = dbcollectiontar.find({"CorrelationId" :correlationId})    
    dbconntar.close()
    
    tar_num_bins = 10
    tar_info     = data_json_tar[0]
    tar_type     = tar_info["Target_ProblemType"]

    for i in input_columns:
        if input_data_types[i] in ["category","object"]:
            raw_input_data[i]=raw_input_data[i].astype("str")
    if(tar_type !="1"):
        raw_input_data[tar_var] = raw_input_data[tar_var].astype("str")
        
    if tar_type == "1":
        desired_value = int(float(desired_value))
        d_val         = desired_value
        target_unique = raw_input_data[tar_var].unique()
        target_unique =[x for x in target_unique if str(x) != 'nan'] 
        
        if   (desired_value > np.max(target_unique)):
            desired_value = np.max(target_unique)
        elif (desired_value < np.min(target_unique)):
            desired_value = np.min(target_unique)
            
        target_minmax           = [np.min(target_unique),np.max(target_unique)]
        target_bins             = list(np.linspace(target_minmax[0],target_minmax[1],num = tar_num_bins))
        raw_input_data[tar_var] = approx.approx_num_bins(target_bins,list(raw_input_data[tar_var]))
        desired_value_approx    = approx.approx_num_bins(target_bins,[desired_value])[0]
        raw_input_data_desired  = raw_input_data[raw_input_data[tar_var] == desired_value_approx]
        raw_input_data_desired  = raw_input_data_desired.dropna()
    
    else:
        desired_value  = str(desired_value)       
        
        if (raw_input_data[tar_var].dtype=="object"):
            raw_input_data_desired = raw_input_data[pd.Series(raw_input_data[tar_var])==desired_value]
        else:
            raw_input_data_desired = raw_input_data[raw_input_data[tar_var]==(desired_value)]      
        raw_input_data_desired = raw_input_data_desired.dropna()
#-----------------------------------------------------------------------------------------------------------------  
    utils.updQdb(correlationId,'P','10',pageinfo,userId,UniId=WFId)
#------------------------------------------------------------------------------------------------------------------
    exc_cols                  = []
    dbproconn,dbprocollection = utils.open_dbconn("WhatIfAnalysis")
    data_whatif               = dbprocollection.find({"CorrelationId" :correlationId,"WFId": WFId})  
    dbproconn.close()

    time.sleep(1)

    l        = data_whatif
    Features = l[0].get('Features')
    if EnDeRequired :
        t = base64.b64decode(Features)           #En......................................
        Features = json.loads(EncryptData.DescryptIt(t))         
    dataF    = {}
    
    for key, value in Features.items():
        if value.get('Selection') == 'False':
            exc_cols.append(value.get('Name'))
        dataF.update({value.get('Name'):value.get('Value')})  

    data_whatif_df      = pd.DataFrame([dataF]) 
    data_whatif_df      = utils.DateTimeStampParser(data_whatif_df)                                  
    data_whatif_df_cols = list(data_whatif_df.columns)
    data_whatif_df_cols.sort()
    
    dbconnfimp,dbcollectionfimp = utils.open_dbconn("ME_FeatureSelection")
    data_json_fimp              = dbcollectionfimp.find({"CorrelationId" :correlationId})    
    dbconn.close()

    fimp    = data_json_fimp[0]["FeatureImportance"]
    fimp_df = pd.DataFrame(fimp)
    if tar_var in list(fimp_df.columns):
        fimp_df = fimp_df.drop(labels=tar_var,axis=1)
    fimp_series = pd.Series(fimp_df.iloc[1])
    fimp_series = fimp_series[input_columns]
    fimp_list   = []
#-----------------------------------------------------------------------------------------------------------------   
    utils.updQdb(correlationId,'P','15',pageinfo,userId,UniId=WFId)
#-----------------------------------------------------------------------------------------------------------------    
    
    if   len(fimp_series) > 5 :
        fimp_max  = fimp_series.sort_values()[-5:]
        fimp_list = list(fimp_max.index)
    elif len(fimp_series)<5 and len(fimp_series)>0:
        fimp_list = list(fimp_series.index) 
    
    fimp_series = fimp_series.sort_values()
    fimp_series = fimp_series.drop(labels = fimp_list)
    fimp_series = list(fimp_series.index)
    fimp_series.reverse()
    for i in fimp_series:
        if (i not in input_columns):
            fimp_series.remove(i)
    
    total_combinations = 50000
    n_combinations     = 1
    num_bins_fimp      = 5
    num_bins           = 3
    train_data_unique  = {}
    num_cols,cat_cols  = [],[]
    success_count_dict = {}
    inf_data_types     = {}
    
    for i in input_columns:
        train_data_unique[i] = input_data_unique[i]
        inf_data_types[i]    = input_data_types[i]
        if((input_data_types[i]=="int64") or (input_data_types[i]=="float64")):
            num_cols.append(i)
        elif(input_data_types[i]=="category"):
            cat_cols.append(i)
    
    for i in fimp_list:
        if input_data_types[i]=="category":
            train_data_unique[i] = list(raw_input_data_desired[i].unique())
            train_data_unique[i] = [x for x in train_data_unique[i] if str(x) != 'nan']
            train_data_unique[i] = [str(x) for x in train_data_unique[i]]
            if "" in train_data_unique[i]:
                train_data_unique[i].remove("")
            success_count_dict.update({i:{}})
            for j in train_data_unique[i]:
                success_count = raw_input_data_desired.groupby(i).count()[tar_var][j]
                total_count   = raw_input_data.groupby(i).count()[tar_var][j]
                success_count_dict[i].update({ j : int((success_count/total_count)*100)})
            
            temp                = pd.Series(success_count_dict[i])
            classes_list_length = len(success_count_dict[i])
            temp_list           = []
            if classes_list_length >5 :
                for j in range(5):
                    class_max = temp.idxmax()
                    temp_list.append(class_max)
                    temp.drop(labels=class_max,inplace=True)
            elif classes_list_length <= 5:
                temp_list = list(temp.index)
            train_data_unique[i] = temp_list
            n_combinations = n_combinations * np.size(train_data_unique[i])
        elif((input_data_types[i]=="int64") or (input_data_types[i]=="float64")):
            train_data_unique[i] = list(raw_input_data[i].unique())
            if "" in train_data_unique[i]:
                train_data_unique[i].remove("")
            train_data_unique[i] = [x for x in train_data_unique[i] if str(x) != 'nan']                
            train_data_unique[i] = list(np.linspace(np.min(train_data_unique[i]),np.max(train_data_unique[i]),num = num_bins_fimp))
            n_combinations       = n_combinations * np.size(train_data_unique[i])
    
    temp_combinations = n_combinations
    
#-----------------------------------------------------------------------------------------------------------------        
    utils.updQdb(correlationId,'P','20',pageinfo,userId,UniId=WFId)    
#-----------------------------------------------------------------------------------------------------------------    

    for i in fimp_series: 
        if ((temp_combinations *3)>total_combinations):
                train_data_unique[i] = data_whatif_df[i][0]
                
        elif input_data_types[i]=="category":
            train_data_unique[i] = list(raw_input_data_desired[i].unique())
            train_data_unique[i] = [x for x in train_data_unique[i] if str(x) != 'nan']
            train_data_unique[i] = [str(x) for x in train_data_unique[i]]
            if "" in train_data_unique[i]:
                train_data_unique[i].remove("")
            success_count_dict.update({i:{}})
            for j in train_data_unique[i]:
                success_count = raw_input_data_desired.groupby(i).count()[tar_var][j]
                total_count   = raw_input_data.groupby(i).count()[tar_var][j]
                success_count_dict[i].update({ j : int((success_count/total_count)*100)})
            temp = pd.Series(success_count_dict[i])
            classes_list_length = len(success_count_dict[i])
            temp_list=[]
            if classes_list_length >3 :
                for j in range(3):
                    class_max=temp.idxmax()
                    temp_list.append(class_max)
                    temp.drop(labels=class_max,inplace=True)
            elif classes_list_length <= 3:
                temp_list = list(temp.index)
            train_data_unique[i] = temp_list
            temp_combinations = temp_combinations * np.size(train_data_unique[i])
        elif((input_data_types[i]=="int64") or (input_data_types[i]=="float64")):
            train_data_unique[i] = list(raw_input_data[i].unique())
            if "" in train_data_unique[i]:
                train_data_unique[i].remove("")
            train_data_unique[i] = [x for x in train_data_unique[i] if str(x) != 'nan']                
            train_data_unique[i] = list(np.linspace(np.min(train_data_unique[i]),np.max(train_data_unique[i]),num = num_bins))
            temp_combinations    = temp_combinations * np.size(train_data_unique[i])
        n_combinations = temp_combinations

#-----------------------------------------------------------------------------------------------------------------    
    utils.updQdb(correlationId,'P','25',pageinfo,userId,UniId=WFId)
#-----------------------------------------------------------------------------------------------------------------    
    
    cartesian_product_data = pd.DataFrame([],columns=input_columns)
    for i in train_data_unique:
        if type(train_data_unique[i]) != list:
            train_data_unique[i] = [train_data_unique[i]]
    td_unique_arrays=[train_data_unique[i] for i in input_columns]
    for i in td_unique_arrays:
        if "" in i:
            i.remove("")
    
    for i in itertools.product(*td_unique_arrays): 
        cartesian_product_data = cartesian_product_data.append(pd.DataFrame([list(i)],columns=input_columns),ignore_index=True)

    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
    dbproconn.close()
    time.sleep(1)
    if  EnDeRequired :
       t = base64.b64decode(data_json[0].get('DataModification'))
       data_json[0]['DataModification']     =  eval(EncryptData.DescryptIt(t))
    binning_data= data_json[0].get('DataModification',{}).get('ColumnBinning')
    if binning_data:    
        if tar_var in binning_data.keys():
            temp = {}
            for  keys,values in binning_data.items():                                
                if values.get('ChangeRequest',{}).get('ChangeRequest') == 'True' and values.get('PChangeRequest',{}).get('PChangeRequest') == 'True':                                        
                    for keys1,values1 in values.items():  
                        if values1.get('Binning') == 'True':
                            Ncat = values1.get('NewName') 
                            subcat =  values1.get('SubCatName')
                            temp.update({subcat:Ncat})
            if tar_var in temp:    
                desired_value = temp[desired_value]
#-----------------------------------------------------------------------------------------------------------------    
    utils.updQdb(correlationId,'P','60',pageinfo,userId,UniId=WFId)
#-----------------------------------------------------------------------------------------------------------------    
    one_record=0
    if(cartesian_product_data.shape[0]==1):
        one_record=1
        filtered_data=cartesian_product_data
    else:
        final=False
    if(cartesian_product_data.shape[0] not in [0,1]): 
        data_spf,predictions = pres_preprocessing.transform_and_predict(cartesian_product_data,input_columns,exc_cols,input_data_types,tar_var,correlationId,WFId,requestId,pageinfo,final)
        if(tar_type !="1"):
            data_spf[tar_var] = data_spf[tar_var].astype("str")
        if (tar_type == "1"):
            data_spf[tar_var] = approx.approx_num_bins(target_bins,list(data_spf[tar_var]))
            if (desired_value_approx > np.max(list(data_spf[tar_var]))):
                desired_value_approx = np.max(list(data_spf[tar_var]))
            elif (desired_value_approx < np.min(list(data_spf[tar_var]))):
                desired_value_approx = np.min(list(data_spf[tar_var]))                         
            data_spf = data_spf[data_spf[tar_var]==desired_value_approx]
            data_spf=data_spf.dropna()
        else:
            if (data_spf[tar_var].dtype=="object"):
                data_spf=data_spf[pd.Series(data_spf[tar_var])==desired_value]
            else:
                data_spf=data_spf[data_spf[tar_var]==desired_value]
            data_spf=data_spf.dropna()
        filtered_data = data_spf
        if(filtered_data.shape[0]==1):
            one_record=1
#-----------------------------------------------------------------------------------------------------------------                
        utils.updQdb(correlationId,'P','65',pageinfo,userId,UniId=WFId)
#-----------------------------------------------------------------------------------------------------------------    
        if(filtered_data.shape[0]!= 0):
            while  (filtered_data.shape[0] not in [0,1]):
                filtered_data               = filters.sp_filter(filtered_data,tar_var)
                if (filtered_data.shape[0]==1) :
                    final=True
                filtered_data,predictions  = pres_preprocessing.transform_and_predict(filtered_data,input_columns,exc_cols,input_data_types,tar_var,correlationId,WFId,requestId,pageinfo,final)
                if(tar_type!="1"):
                    filtered_data[tar_var] = filtered_data[tar_var].astype("str")
                if (tar_type == "1"):
                    filtered_data[tar_var] = approx.approx_num_bins(target_bins,list(filtered_data[tar_var])) 
                    if (desired_value_approx > np.max(list(filtered_data[tar_var]))):
                        desired_value_approx = np.max(list(filtered_data[tar_var]))
                    elif (desired_value_approx < np.min(list(filtered_data[tar_var]))):
                        desired_value_approx = np.min(list(filtered_data[tar_var]))
                    filtered_data = filtered_data[filtered_data[tar_var]==desired_value_approx]
                    filtered_data = filtered_data.dropna()
                else: 
                    if (filtered_data[tar_var].dtype=="object"):
                        filtered_data = filtered_data[pd.Series(filtered_data[tar_var])==desired_value]
                    else:
                        filtered_data=filtered_data[filtered_data[tar_var]==desired_value]
                    filtered_data = filtered_data.dropna()
            if(one_record!=1):
                utils.updQdb(correlationId,'P','95',pageinfo,userId,UniId=WFId)
                filtered_data.drop(labels = tar_var,axis=1,inplace=True)
                filtered_data_cols =list(filtered_data.columns)
                filtered_data_vals = list(filtered_data.values[0])
                filtered_data_dict = dict(zip(filtered_data_cols,filtered_data_vals))                
                pred_dict=predictions["Prediction"]
                if (tar_type == "1"):
                    pred_dict.update({"Prediction":round(d_val,2)})
                predictions.update({"Prediction":pred_dict})
        else:
            predictions = None
    elif(one_record==1):
        if tar_var in list(filtered_data.columns):
            filtered_data.drop(labels = tar_var,axis=1,inplace=True)
        final=True
        filtered_data,predictions  = pres_preprocessing.transform_and_predict(filtered_data,input_columns,exc_cols,input_data_types,tar_var,correlationId,WFId,requestId,pageinfo,final)
    else:
         predictions = None
    dbproconn,dbprocollection = utils.open_dbconn("SSAI_IngrainRequests")
    data_json_model = dbprocollection.find({"CorrelationId" :correlationId,"RequestId":requestId,"pageInfo":pageinfo}) 
    dbproconn.close()
    model_info = list(data_json_model)
    model = model_info[0]["ModelName"]
    _,_,ProblemType,_ = utils.get_pickle_file(correlationId,model,'MLDL_Model')
    
    if EnDeRequired:
        predictions = EncryptData.EncryptIt(json.dumps(predictions))
        
    dbconn, dbcollection = utils.open_dbconn("PrescriptiveAnalyticsResults")
    Id1 = str(uuid.uuid4())

    dbcollection.insert_many([{"_id" : Id1,  
                                "CorrelationId":correlationId,
                                "WFId": WFId,
                                "ScenarioName":"",
                                "Temp" :"False",
                                "BulkTest" : False,
                                "Model":model,
                                "ProblemType":ProblemType,
                                "Target":str(tar_var),
                                "Predictions":predictions,
                                "CreatedBy":userId,
                                "CreatedOn":str(datetime.datetime.now())
                               # "Predicted_Value_TC": None if ProblemType!='Text_Classification' else predicted_value_TC[0],
                                #"Predict_Probability_TC": None if ProblemType!='Text_Classification' else predict_fn_TC[0]
                                }])
    dbconn.close()
    utils.updQdb(correlationId,'C','100',pageinfo,userId,UniId=WFId)