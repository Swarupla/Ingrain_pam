import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
from datapreprocessing import DataEncoding
from datapreprocessing import Data_Modification
import pandas as pd 
import time
import datetime
import re 
import numpy as np  
from SSAIutils import EncryptData #En............................
import base64
from scipy.stats import wasserstein_distance
import configparser,os
import file_encryptor
import uuid
from dataqualitychecks import data_quality_check as dq
import platform
from collections import Counter

config = configparser.RawConfigParser()
#configpath = "/var/www/monteCarloPython/pythonconfig.ini"

#if platform.system() == 'Linux':
#    conf_path = 'C:/Personal/IngrAIn_Python/main/pythonconfig.ini'
#    work_dir = '/IngrAIn_Python'
#elif platform.system() == 'Windows':
#    conf_path = 'C:\\Personal\\Ingrain\\IngrAIn_Python\\main\\pythonconfig.ini'
#    work_dir = '\IngrAIn_Python'

if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'
try:
    config.read(conf_path)
except UnicodeDecodeError:
    #print("Decrypting Config File : monitoring_utils.py")
    config = file_encryptor.get_configparser_obj(conf_path)

def predict_target(tf_data,correlationId,input_columns,input_data_types,target_variable,requestId,pageinfo,model):
    EnDeRequired = utils.getEncryptionFlag(correlationId)

    data=tf_data.copy()
#    dbproconn,dbprocollection = utils.open_dbconn("SSAI_IngrainRequests")
#    data_json_model = dbprocollection.find({"CorrelationId" :correlationId,"RequestId":requestId,"pageInfo":pageinfo}) 
#    dbproconn.close()
#    model_info = list(data_json_model)
#    model = model_info[0]["ModelName"]
    
    dbconn,dbcollection = utils.open_dbconn("SSAI-savedModels")
    data_savedMod = dbcollection.find({"CorrelationId" :correlationId}) 
    count = dbcollection.count_documents({"CorrelationId" :correlationId}) 
    trans = {}
    for indx in range(count):
        if data_savedMod[indx].get('FileType') == ('StandardScaler' or 'Normalizer'):            
            trans.update({data_savedMod[indx].get('FileName') :data_savedMod[indx].get('FilePath') })            
    dbconn.close()
    time.sleep(1)
    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))   #DE.....................................
    if EnDeRequired :
         t = base64.b64decode(data_json[0].get('DataModification'))
         data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
#    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
#    data_json = dbprocollection.find({"CorrelationId" :correlationId}) 
#    dbproconn.close()
#    time.sleep(1)
#    
    #Scalers 
    Features = data_json[0].get('DataModification',{}).get('Features')  ##########################################################
    if "Interpolation"in Features:
        del Features["Interpolation"]
    for key,value in Features.items():
        data_transformations_data = value.get('Skewness')
        if data_transformations_data:
            value_t1 = [key2 for key2,value2 in data_transformations_data.items() if value2=='True' and (data_transformations_data.get('ChangeRequest') == 'True' and data_transformations_data.get('PChangeRequest') == 'True') and (key2 != 'ChangeRequest' and key2!='PChangeRequest')]            
            if value_t1:                
                if value_t1[0] == 'Standardization':
                    wtd = 'StandardScaler'
                elif value_t1[0] == 'Normalization':
                    wtd = 'Normalizer'                   
                file_path = None   
                file_path = trans.get(str(wtd)+'-'+str(key))                
                if file_path !=None:                        
                    transm,_,_,_=utils.get_pickle_file(FilePath = file_path)                    
                    data= Data_Modification.scaler_dec(data,key,wtd,transm)                    
     
    if data.isnull().values.any():
    #        data = data.fillna(value=0) 
        data=data.dropna(axis=0)
    
    binning_data= data_json[0].get('DataModification',{}).get('ColumnBinning')
    if binning_data:
        if target_variable in binning_data.keys():
            del binning_data[target_variable]
        temp = {}
        for  keys,values in binning_data.items():                                
            if values.get('ChangeRequest',{}).get('ChangeRequest') == 'True' and values.get('PChangeRequest',{}).get('PChangeRequest') == 'True':                                        
                for keys1,values1 in values.items():  
                    if values1.get('Binning') == 'True':
                        Ncat = values1.get('NewName') 
                        subcat =  values1.get('SubCatName')
                        temp.update({subcat:Ncat})
                Ucat = list(set(temp.values()))      
                for indx in range(len(Ucat)):
                    column = str(keys)                                       
                    subcat = [key2 for key2,value2 in temp.items() if value2 == Ucat[indx]]    
                    data.loc[data[column].isin(subcat),column]=Ucat[indx]
    
    
    '''CHANGES START HERE'''
    #print("adding new features") 
    map_encode_new_feature={}
    features_created = []
    
    dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
    data_features = dbcollection.find({"CorrelationId" :correlationId}) 
    try: 
        features_created          = data_features[0].get("Features_Created")
        encode_new_feature = data_features[0].get("Map_Encode_New_Feature")
    except Exception:
        features_created = []
    
    if len(features_created)>0:
        if len(encode_new_feature)>0:
            for item in encode_new_feature:
                map_encode_new_feature[item] = {'attribute': 'Nominal','encoding': 'Label Encoding','ChangeRequest': 'True','PChangeRequest': 'False'}
        
    Data_to_Encode=data_json[0].get('DataEncoding')  
    '''CHANGES START HERE'''
    if len(map_encode_new_feature)>0:
        Data_to_Encode.update(map_encode_new_feature)
    '''CHANGES END HERE'''
    OHEcols = []
    LEcols = []
    encoders = {}
#    encoders1={}

    for keys,values in Data_to_Encode.items():                
        if values.get('encoding') == 'One Hot Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
            OHEcols.append(keys)
        elif values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
            LEcols.append(keys)
    
    OHEcols=[value for value in OHEcols if value in input_columns]                       
    if OHEcols:      
        ohem,_,enc_cols,_ = utils.get_pickle_file(correlationId,FileType='OHE')
        data,data_ohe = DataEncoding.one_hot_dec(ohem,data,OHEcols,enc_cols)
        encoders={'OHE':{ohem:{'EncCols':enc_cols,'OGCols':OHEcols}}}
    
    LETarget = 'None'
    
    if target_variable in LEcols:
        if len(LEcols)==1:          
            lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
            LEcols.remove(target_variable)  
            LETarget = 'True'
        else:
            lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
            LEcols.remove(target_variable)  
            Lenc_cols.remove(target_variable+'_L')
            LETarget = 'True'
            classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
    elif LEcols:
        lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
        classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
    
    LEcols=[value for value in LEcols if value in input_columns]
    
    if len(LEcols)>0:            
        #data,data_le = DataEncoding.Label_Encode_Dec(lencm,data,LEcols)
        data,data_le,classMappingUpdated = DataEncoding.Label_Encode_Dec_modified(lencm,data,LEcols)
        encoders.update({'LE':{lencm:Lenc_cols}})
        if len(classMapping)!=len(classMappingUpdated):
            for item in classMappingUpdated.keys():
                #print(item)
                if item not in classMapping.keys():
                    #print(item)
                    lencm.classes_=np.append(lencm.classes_,item)
    else:
        pass
        
    
    if data.isnull().values.any():
        data = data.fillna(value=0) 
    
    LEcols=[value for value in LEcols if value in input_columns ]
    drop_cols = OHEcols + LEcols
    
    if target_variable in list(data.columns):
        data = data.drop(target_variable,axis=1)
    
    if drop_cols:
        data_t = data.drop(drop_cols,axis=1)
    else:
        data_t = data  
    
    #if data_t.isnull().values.any():
    #    utils.updQdb(correlationId,'E',"Their are nulls in data, preprocess data, Missing values",pageInfo,userId,UniId = WFId)            
    #    utils.save_Py_Logs(logger,correlationId)
    #    return    
            
    temp_col1 = list(data_t.columns[data_t.dtypes == 'datetime64[ns]'])
    
    temp_col2 = list(data_t.columns[(data_t.dtypes == 'object')])
    a=[]
    for i in temp_col2:
        if input_data_types[i] in ["int64","float64"]:
            a.append(i)
            
    data_types={}
    for i in temp_col2:
        data_types[i] = input_data_types[i]
    data_t =data_t.astype(data_types)
    
    temp_col2 = list(set(temp_col2)-set(a))
    temp_col=temp_col1+temp_col2
    
    if len(temp_col) != 0:  
        data_t = data_t.drop(temp_col,axis=1)
           
    
    #if ('object' or 'datetime64[ns]') in list(data_t.dtypes):
    #    data_tdtypes = dict(data_t.dtypes)
    #    utils.logger(logger,correlationId,'INFO', ('data dtypes'+str(data_tdtypes)))
    #    utils.updQdb(correlationId,'E',"Their are strings/datatime stamps in data, preprocess data, Data Encoding",pageInfo,userId,UniId = WFId)                
    #    utils.save_Py_Logs(logger,correlationId)
    #    return   
    
    MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,model,'MLDL_Model')  
    
    #######
#    bulk = False
    #######
    if ProblemType == "classification" or ProblemType== "Multi_Class":
        if model!='SVM Classifier':
                MLDL_Model =  MLDL_Model.best_estimator_
        if model=='XGBoost Classifier':
            traincols=MLDL_Model.get_booster().feature_names
            
        test_data = data_t[traincols]
    elif ProblemType == "regression":
        test_data = data_t[traincols]
    testdat=test_data.copy()
    pred_series=MLDL_Model.predict(testdat)
    if (ProblemType !="regression"):
        tf_data[target_variable]=lencm.inverse_transform(pred_series)
    else:
        tf_data[target_variable]=(pred_series)
    #        test_data_cols = list(test_data.columns)
    final_data = tf_data.copy()
    return final_data

def monitoring_parameters(prod_data,base_data,actual_prod_target,actual_train_target,pred_target,target_variable,target_type,top_inf,deployed_accuracy,input_columns,input_data_types):
    
    prod_data_inf = prod_data[input_columns]
    base_data_inf = base_data[input_columns]
    #####################Target_Variance Calculation###########################
    
    t_tar = list(actual_train_target)
    n_tar = list(actual_prod_target)
    
    t_uni = list(actual_train_target.unique())
    n_uni = list(actual_prod_target.unique())
    tar_wd = 0 
    new_classes = 0
    new_count=0
    target_drift=0
    if(target_type=="1"):
        tar_wd = abs(wasserstein_distance(t_tar,n_tar))
        target_variance = (np.var(n_tar)-np.var(t_tar))/np.var(t_tar)
        target_mean = ((np.mean(n_tar)-np.mean(t_tar)/np.mean(t_tar)))
    else:
        target_count = dict(Counter(n_tar))
        for i in n_uni: 
            if i not in t_uni:
                new_count += target_count[i]
                new_classes+=1
        if new_classes>0:
            target_drift = 20 + (new_count/(len(t_tar)+len(n_tar)))*100
            if target_drift>100:
                target_drift =100
    if target_type=="1":
        if str(target_variance)=="nan":
            target_variance = 0
        if abs(target_mean)>20 and tar_wd > 0:
            target_drift = abs(target_mean)*100
        if abs(target_variance)>20 and tar_wd>0:
            target_drift = abs(target_variance)*100
        else:
            target_drift = abs(target_variance)*100
    ###########################################################################
    
    #####################Input_drift Calculation###########################
    input_drift = 0
    input_variance = {}
    ip_wd = []
    ip_new_classes = 0
    ip_new_count=0
    for i in top_inf:
        cat_var = []
        t_col = list(base_data_inf[i])
        n_col = list(prod_data_inf[i])
       
        t_col_unique = list(base_data_inf[i].unique())
        n_col_unique = list(prod_data_inf[i].unique())
       
        if(input_data_types[i] in ["int32","float32","int64","float64"]):
            ip_wd.append(abs(wasserstein_distance(t_col,n_col)))
            inf_var  = (np.var(n_col)-np.var(t_col))/np.var(t_col)
            inf_mean = (np.mean(n_col)-np.mean(t_col)/np.mean(t_col))
            if "nan" == str(inf_mean):
                inf_mean = 0
            if "nan" == str(inf_var):
                inf_var = 0
            if inf_var > inf_mean:
                input_variance.update({i:abs(inf_var)})
            elif inf_var<inf_mean:
                input_variance.update({i:abs(inf_mean)})
        elif (input_data_types[i] == "category"):
            ip_class_count = dict(Counter(n_col))
            for j in n_col_unique:
                if j not in t_col_unique:
                    ip_new_count += ip_class_count[j]
                    ip_new_classes +=1
                    cat_var.append((ip_new_count/(len(t_col)+len(n_col)))*100)
                else:
                    cat_var.append(0)
            input_variance.update({i:np.sum(cat_var)})
       
    for i in top_inf:
        input_drift += round(float(input_variance[i]),2)
    input_drift = input_drift/len(top_inf)
    if ip_new_classes>0:
        input_drift+=20
    if input_drift>100:
        input_drift=100
		
# if(target_type!="1"):
# if ip_new_classes>0:
# input_drift = 20 + np.sum(input_variance)/len(top_inf)
# else:
# input_drift = np.sum(input_variance)/len(top_inf)
# if input_drift>100:
# input_drift=100
# if target_type=="1":
# if "nan"== str(input_variance[0]) :
# input_variance = [0]*len(input_variance)
# if len(input_variance)>0: input_variance = np.average(input_variance)
# if len(input_mean)>0: input_mean = np.average(input_mean)
# if abs(input_mean)>20:
# input_drift = abs(input_mean)*100
# if abs(input_variance)>20:
# input_drift = abs(input_variance)*100
# else:
# input_drift = abs(input_variance)*100

    #######################################################################
    
    #########################Accuracy Change Calculation#######################
    if target_type == "1":
        y_bar = np.mean(actual_prod_target)
        y_bar = y_bar * len(actual_prod_target)
        r2_num = np.linalg.norm(pred_target-y_bar,ord=2)
        r2_den = np.linalg.norm(actual_prod_target-y_bar,ord=2)
        prod_accuracy = ((r2_num/r2_den))*100
    else:
        right_predictions = 0
        pred_target =list(pred_target)
        actual_prod_target = list(actual_prod_target)
        for i in range(len(pred_target)):
            if (pred_target[i] ==actual_prod_target[i]):    right_predictions+=1
        prod_accuracy =( right_predictions/len(pred_target))*100
    if str(prod_accuracy) == "nan":
        prod_accuracy = deployed_accuracy
    threshold_accuracy = deployed_accuracy - (deployed_accuracy*0.15)
    
    ################################Data-Quality Calculation######################
    data_quality = Data_Quality_Score(prod_data,target_variable)
    ##############################################################################
    model_health = "Healthy"
    if input_drift>20 or target_drift>20 or prod_accuracy<float(threshold_accuracy) :
        model_health = "Unhealthy"
    param_details = {}
    param_details.update({"ModelHealth":model_health})
    param_details.update({"Accuracy":{
            "ThresholdValue": threshold_accuracy,
            "CurrentValue"  : prod_accuracy
            }})
    param_details.update({"InputDrift":{
        "ThresholdValue": 20,
        "CurrentValue"  : input_drift
        }})
    param_details.update({"TargetVariance":{
            "ThresholdValue": 15,
            "CurrentValue"  : target_drift
            }})
    param_details.update({"DataQuality":{
            "ThresholdValue": 75,
            "CurrentValue"  : data_quality
            }})
    return param_details

def prepare_update_monitoring_graphdata(CorrelationId,LastRequestId,RequestId,monitoring_parameters,userId):
    date_object = datetime.datetime.now()
    date_string = date_object.strftime("%Y-%m-%d %H:%M:%S")
    dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
    data_json = dbcollection.find({"CorrelationId":CorrelationId})
    k=data_json[0]
    clientuid= k.get("ClientUId")
    dcuid = k.get("DeliveryConstructUID")
    created_date = k.get("CreatedOn")
    modified_date = date_string
    
    try:
        dbconn,dbcollection = utils.open_dbconn("ModelMetrics")
        data_json = dbcollection.find({"CorrelationId":CorrelationId,"RequestId":LastRequestId})
        k=data_json[0]
        accuracy = k.get("Accuracy")
        accuracy["ThresholdValue"] = monitoring_parameters["Accuracy"]['ThresholdValue']
        accuracy['CurrentValue'] = monitoring_parameters["Accuracy"]['CurrentValue']
        accuracy["GraphData"]["Date"].append(date_string)
        accuracy["GraphData"]["MetricValue"].append(monitoring_parameters["Accuracy"]["CurrentValue"])
        
        inputdrift = k.get("InputDrift")
        inputdrift["ThresholdValue"] = monitoring_parameters["InputDrift"]['ThresholdValue']
        inputdrift['CurrentValue'] = monitoring_parameters["InputDrift"]['CurrentValue']
        inputdrift["GraphData"]["Date"].append(date_string)
        inputdrift["GraphData"]["MetricValue"].append(monitoring_parameters["InputDrift"]["CurrentValue"])
        
        targetvar = k.get("TargetVariance")
        targetvar["ThresholdValue"] = monitoring_parameters["TargetVariance"]['ThresholdValue']
        targetvar['CurrentValue'] = monitoring_parameters["TargetVariance"]['CurrentValue']
        targetvar["GraphData"]["Date"].append(date_string)
        targetvar["GraphData"]["MetricValue"].append(monitoring_parameters["TargetVariance"]["CurrentValue"])
        
        dataquality = k.get("DataQuality")
        dataquality["ThresholdValue"] = monitoring_parameters["DataQuality"]['ThresholdValue']
        dataquality['CurrentValue'] = monitoring_parameters["DataQuality"]['CurrentValue']
        dataquality["GraphData"]["Date"].append(date_string)
        dataquality["GraphData"]["MetricValue"].append(monitoring_parameters["DataQuality"]["CurrentValue"])
        
#        metrics = {"Accuracy":accuracy,"InputDrift":inputdrift,"TargetVariance":targetvar,"DataQuality":dataquality,"ModifiedOn":modified_date}
        dbcollection.insert({
                "ClientUId": clientuid,
                "DeliveryConstructUID":dcuid,
                "RequestId":RequestId,
                "CorrelationId":CorrelationId,
                "ModelHealth":monitoring_parameters["ModelHealth"],
                "Accuracy" : accuracy,
                "InputDrift":inputdrift,
                "TargetVariance":targetvar,
                "DataQuality":dataquality,
                "CreatedOn": created_date,
                "CreatedByUser":userId,
                "ModifiedOn":modified_date,
                "ModifiedByUser":userId
                    })
        dbconn.close()
    except:
        dbconn,dbcollection = utils.open_dbconn("ModelMetrics")
        k=monitoring_parameters
        
        accuracy = k.get("Accuracy")
        accuracy.update({"GraphData":{
                "Date":[date_string],
                "MetricValue":[monitoring_parameters["Accuracy"]["CurrentValue"]]
                }})
        
        inputdrift = k.get("InputDrift")
        inputdrift.update({"GraphData":{
                "Date":[date_string],
                "MetricValue":[monitoring_parameters["InputDrift"]["CurrentValue"]]
                }})
        targetvar = k.get("TargetVariance")
        targetvar.update({"GraphData":{
                "Date":[date_string],
                "MetricValue":[monitoring_parameters["TargetVariance"]["CurrentValue"]]
                }})
        dataquality = k.get("DataQuality")
        dataquality.update({"GraphData":{
                "Date":[date_string],
                "MetricValue":[monitoring_parameters["DataQuality"]["CurrentValue"]]
                }})
        dbcollection.insert_one({
                "ClientUId": clientuid,
                "DeliveryConstructUID":dcuid,
                "RequestId":RequestId,
                "CorrelationId":CorrelationId,
                "ModelHealth":monitoring_parameters["ModelHealth"],
                "Accuracy" : accuracy,
                "InputDrift":inputdrift,
                "TargetVariance":targetvar,
                "DataQuality":dataquality,
                "CreatedOn": created_date,
                "CreatedByUser":userId,
                "ModifiedOn":modified_date,
                "ModifiedByUser":userId
                    })
        dbconn.close()
        
def Data_Quality_Score(data, target, Prob_type=None, DfModified=None, flag=None, text_cols=None, id_cols=None,
                        OrgnalTypes=None, scale_v=None):
    if flag == None:
        empty_cols = [col for col in data.columns if data[col].dropna().empty]
        data.drop(columns=empty_cols, inplace=True)
        OrgnalTypes = pd.Series([data[col].dtype.name for col in data.columns], index=data.columns).to_dict()
        data_copy, id_cols, date_cols, text_cols, incorrect_date = dq.identifyDaType(data.copy(), target, Prob_type)
        if Prob_type == 'TimeSeries':
            OrgnalTypes[target] = data_copy[target].dtype.name
        cols_to_remove = id_cols + date_cols + text_cols
        corrDict, corelated_Series = dq.CheckCorrelation(data_copy, cols_to_remove, target)
       

    elif flag == 2:
        id_cols = []
        text_cols = []
        date_cols = list(data.select_dtypes('datetime64[ns]').columns)
        OrgnalTypes = pd.Series([data[col].dtype.name for col in data.columns], index=data.columns).to_dict()
        incorrect_date = pd.Series(data=0, index=data.columns)
        corrDict, corelated_Series = dq.CheckCorrelation(data.copy(), date_cols, target)
        data_copy = data.copy()


    else:
        date_cols = list(data.select_dtypes('datetime64[ns]').columns)
        incorrect_date = pd.Series(data=0, index=DfModified.columns)
        cols_to_remove = id_cols + date_cols + text_cols
        corrDict, corelated_Series = dq.CheckCorrelation(data.copy(), cols_to_remove, target)
        temp = {}
        for col in DfModified.columns:
            temp[col] = OrgnalTypes[col]
        OrgnalTypes = temp
        data_copy = DfModified.copy()
    Skewed_cols = dq.Check_Skew(data_copy)
    Outlier_Data_Count, Outlier_Data_Percent, percent_unique, \
    count_unique, percent_missing, count_missing, Outlier_Dict = dq.CalculateStastics(data_copy)
    ImBalanced_col, types_of_data, ImbalanceDict, Skewed = dq.CheckImbalance(data_copy, target, Skewed_cols)
    Q_Scores, Q_Info = dq.calculate_QScore(data_copy, percent_missing, Outlier_Data_Percent, Skewed, incorrect_date,
                                        ImBalanced_col)
    data_quality_score = np.average(Q_Scores)
    return data_quality_score

def prepare_prod_data(CorrelationId,deployed_date,data,uid,uid_list):
    offlineutility = utils.checkofflineutility(CorrelationId)
    if offlineutility:
        raw_data = utils.data_from_chunks_offline_utility(corid=CorrelationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
    else:
        raw_data = utils.data_from_chunks(CorrelationId,"PS_IngestedData")
    tmp = list(raw_data["DateColumn"])
    
    try:
        raw_data["DateColumn"] = [i.tz_localize(None) for i in tmp]
        raw_data = raw_data[(raw_data["DateColumn"])<=(deployed_date)]

    except:
        raw_data["DateColumn"] = [pd.to_datetime(i).tz_localize(None) for i in list(raw_data["DateColumn"])]
        deployed_date = datetime.datetime.strptime(str(deployed_date), '%Y-%m-%d %H:%M:%S').strftime('%Y-%d-%m %H:%M:%S')
        raw_data = raw_data[(raw_data["DateColumn"])<=(deployed_date)]

    train_uids = list(raw_data[uid].unique())
    if None in train_uids :
        train_uids = list(set(train_uids)-set([None]))
    base_data = data[data[uid].isin(train_uids)]
    prod_data = data[data[uid].isin(uid_list)]
    return base_data,prod_data