# -*- coding: utf-8 -*-
"""
Created on Sat Jan 23 17:27:49 2021

@author: shrayani.mondal
"""
import time
start =  time.time()
import platform
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
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


import datetime
from datapreprocessing import Textclassification
from SSAIutils import utils
import pandas as pd
import numpy as np
from sklearn import preprocessing
import base64
from SSAIutils import EncryptData
from pandas import Timestamp
end = time.time()

def Normalizer(data):
    scaler = preprocessing.Normalizer().fit(data)
    data = scaler.transform(data)
    return data

def getDataPreprocessingDetails(correlationId,targetVar):
    
    EnDeRequired = utils.getEncryptionFlag(correlationId)
    
    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
    dbproconn.close()
    
    if EnDeRequired :
         t = base64.b64decode(data_json[0].get('DataModification'))
         data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
         
    #binning metadata
    binning_data= data_json[0].get('DataModification',{}).get('ColumnBinning')
    binnedColDict = {}
    if binning_data:        
        for  keys,values in binning_data.items():                                
            if values.get('ChangeRequest',{}).get('ChangeRequest') == 'True' and (values.get('PChangeRequest',{}).get('PChangeRequest') == 'True' or values.get('PChangeRequest',{}).get('PChangeRequest') == ''):                                        
                for keys1,values1 in values.items():  
                    if values1.get('Binning') == 'True':
                        Ncat = values1.get('NewName') 
                        subcat =  values1.get('SubCatName')
                        binnedColDict.update({subcat:Ncat})
                        
    #encoding metadata
    Data_to_Encode = data_json[0].get('DataEncoding')
    
    LEcols = []
    for keys,values in Data_to_Encode.items():                
        if values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
            LEcols.append(keys)
    
    LETarget = 'None'
    lencm = 'None'
    if LEcols:
        lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
        if targetVar in LEcols or targetVar in Lenc_cols:
            LETarget = 'True'
    return binnedColDict,LETarget,lencm
    
def main(correlationId,pageInfo,userId):
    dbconn,dbcollection = utils.open_dbconn('SSAI_DeployedModels')
    data_json = list(dbcollection.find({"CorrelationId":correlationId}))
    if data_json :
        selectedModel = data_json[0].get("ModelVersion") 
        pType = data_json[0].get("ModelType")             
        try:
            language = data_json[0].get('Language').lower()
        except Exception:
            language = 'english'
        dbconn.close()
        
        # Pulling data
        utils.updQdb(correlationId,'P','10',pageInfo,userId)
        data = utils.data_from_chunks(correlationId,'DE_PreProcessedData')
        
        #Get feature selection parameters and target
        featureParams = utils.getFeatureSelectionVariable(correlationId)
        targetVar = utils.getTargetVariable(correlationId)
        UniqueIdentifier,UIDList = utils.getUniqueIdentifier(correlationId)
        selectedColumn = utils.getSelectedColumn(featureParams["selectedFeatures"],data.columns)
        
        #ensure target column is not selected
        selectedColumn = list(set(selectedColumn+[targetVar]))
        selectedColumn.remove(targetVar)
        
        dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
        data_json = dbcollection.find({"CorrelationId":correlationId})
        problemTypeflag=data_json[0].get("NLP_Flag")
        clustering_flag = data_json[0].get("Clustering_Flag")
        
        if isinstance(problemTypeflag,type(None)):
            problemTypeflag=False
        if isinstance(clustering_flag,type(None)):
            clustering_flag=True
        
        if problemTypeflag and not clustering_flag:
            vectorized_df = Textclassification.clustering_optional(data['All_Text'],correlationId,language=language)
        
        if problemTypeflag and not clustering_flag:
            trainData = data[selectedColumn].join(vectorized_df)
            trainData = trainData.drop('All_Text',axis=1)
        else:
            trainData = data[selectedColumn]
        
        #remove uniqueId
        if UniqueIdentifier and UniqueIdentifier in list(trainData.columns):
            UniqueIdentifierList = trainData[UniqueIdentifier].tolist()
            trainData.drop(UniqueIdentifier, axis=1, inplace=True)
        else:
            UniqueIdentifierList = None
        
        #get the deployed model
        dbconn,dbcollection = utils.open_dbconn('SSAI_RecommendedTrainedModels')
        get_version_record = dbcollection.find({"CorrelationId" :correlationId,"modelName":selectedModel}) 
        version_list=[]   
        for i in range(0,get_version_record.count()):
                     version_list.append(get_version_record[i].get('Version'))
        
        if max(version_list)!=0:
            version=max(version_list)
        else:
            version=0

        MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,selectedModel,'MLDL_Model',version=version)
        if ProblemType == "classification" or ProblemType== "Multi_Class":
            if selectedModel!='SVM Classifier':		
                MLDL_Model =  MLDL_Model.best_estimator_
            if selectedModel=='XGBoost Classifier':
                traincols=MLDL_Model.get_booster().feature_names
                if not clustering_flag:
                    traincols2 = [int(x) for x in traincols[-vectorized_df.shape[1]:]]
                    traincols = traincols[:-vectorized_df.shape[1]] + traincols2
            test_data = trainData[traincols]
        elif ProblemType == "regression":
            test_data = trainData[traincols]
        
        if ProblemType == "classification" or ProblemType== "Multi_Class" :
            dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
            filterdata_json = dbcollection.find({"CorrelationId" :correlationId})    
            dbconn.close()
            target_classes = filterdata_json[0].get('UniqueTarget')
        
        #remove uniqueId from traincols    
        if UniqueIdentifier and UniqueIdentifier in traincols:
            traincols.remove(UniqueIdentifier)
        
        binnedColDict,LETarget,lencm = getDataPreprocessingDetails(correlationId,targetVar)
        
        #get actual target name
        dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId" :correlationId}))
        dbconn.close()
        Atarget_variable = data_json[0].get('TargetColumn')
        
        #predictions starts
        predictions=[]
        for indx,row in enumerate(test_data.iterrows()):
            Atest_pred = 'None'
            temp_pred = {}
            Data = dict(row[1])   
            
            testD = pd.DataFrame([Data],columns=traincols)
            if selectedModel =='SVM Regressor':
                testD = Normalizer(testD)
                testD = pd.DataFrame(testD,columns=traincols)
            test_pred = MLDL_Model.predict(testD)
            if UniqueIdentifierList:
                temp_pred.update({"ID": UniqueIdentifierList[indx]})
            if LETarget == 'True':
                Atest_pred= lencm.inverse_transform([int(test_pred)])
            else:
                Atest_pred = test_pred
            if ProblemType == "classification" or ProblemType== "Multi_Class" :				
                 temp_pred.update({"Target": Atest_pred[0]}) 
            else:
                 temp_pred.update({"Target": round(float(Atest_pred[0]),2)})			
            if ProblemType == "classification" or ProblemType=="Multi_Class":
                 common_classes = [x for x in lencm.classes_ if x in target_classes]

                 target_binnned_classes = set(target_classes).intersection(set(binnedColDict.keys()))
                 binned_target_list = []
                 if len(binnedColDict.keys())>0:
                     if len(target_binnned_classes)>0:
                         for item,value in binnedColDict.items():
                             if item in target_binnned_classes:
                                 binned_target_list.append(value)
                 binned_target_list = list(set(binned_target_list))
                 common_classes = common_classes + binned_target_list

                 common_classes1 =[]
                 for item in MLDL_Model.classes_:
                     if lencm.inverse_transform([int(item)]) in common_classes:
                         common_classes1.append(lencm.inverse_transform([int(item)])[0])
                 if len(common_classes1) > 0:
                     common_classes = common_classes1 

                 test_prob = MLDL_Model.predict_proba(testD)
                 dict1={}
                 if selectedModel=='SVM Classifier' and len(common_classes1) > 2:
                     sortorder = np.argsort(MLDL_Model.decision_function(testD))[0][::-1]
                     test_prob[0].sort()	
                 for i in range(0,test_prob.shape[1]):
                     if selectedModel!='SVM Classifier' or len(common_classes1) < 3:
                        dict1[common_classes[i]]=float(test_prob[0][i])
                     else:
                        dict1[common_classes[sortorder[i]]]=float(test_prob[0][::-1][i])
                 
                 dict1=dict(sorted(dict1.items(),key=lambda t : t[1] , reverse=True))                
                 dict1={k:round(round(v,4)*100,3) for k,v in dict1.items()}
                 temp_pred.update({"TargetScore": dict1})
                 
            predictions.append(temp_pred)
        
        utils.updQdb(correlationId,'P','90',pageInfo,userId)
        
        #insert predictions
        dbconn,dbcollection = utils.open_dbconn('SSAI_PredictedData')
        dbcollection.update_many({"CorrelationId"     : correlationId},
                            { "$set":{      
                               "CorrelationId"     : correlationId,
                               "ProblemType"       : pType,
                               "ModelVersion"      : selectedModel,
                               "TargetName"        : Atarget_variable,
                               "CreatedByUser"     : userId,
                               "CreatedOn"         : str(datetime.datetime.now()),
                               "PredictedResult"   : predictions
                               }},upsert=True)
        dbconn.close()
        
        #update QDB to 100 percent
        utils.updQdb(correlationId,'C','100',pageInfo,userId)
        
    else:
        raise Exception ("Unable to find model for corresponding correlationId")
    
    
try:
    correlationId = sys.argv[1]
    requestId = sys.argv[2]
    pageInfo = sys.argv[3]
    userId = sys.argv[4]
    logger = utils.logger('Get',correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+' secs',str(requestId)))
    utils.updQdb(correlationId,'P','0',pageInfo,userId)
    utils.logger(logger, correlationId, 'INFO', ('Predict Cascade started at '  + str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    main(correlationId,pageInfo,userId)
    utils.logger(logger, correlationId, 'INFO', ('Predict Cascade Completed at '  + str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

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

    
    