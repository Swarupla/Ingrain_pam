# -*- coding: utf-8 -*-
"""
Created on Tue Jul 30 06:55:15 2019

@author: harsh.nandedkar
"""

# -*- coding: utf-8 -*-
"""
Created on Thu Feb 21 12:25:56 2019

@author: sravan.kumar.tallozu
"""
import numpy as np
import logging
import xgboost as xg
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
from datapreprocessing import Train_Cross_Validate
from evaluation import Eli5_M
import time
from evaluation import M_Evaluation
from evaluation import MultiClass_Evaluation
import pandas as pd
from datapreprocessing import Textclassification
from pandas import Timestamp
from datetime import datetime

# ProblemType

# Model Random Search Params

def xgb_RS_params(correlationId):
    pType,unique_target = utils.fetchProblemType(correlationId)
    
    if pType == 'Classification' :
        params={'gamma':[i/10.0 for i in range(0,5)],
            'subsample':[i/10.0 for i in range(6,10)],
            'colsample_bytree':[i/10.0 for i in range(6,10)],
            'reg_alpha':[0, 0.001, 0.005, 0.01, 0.05],
            "learning_rate"    : [0.01, 0.03, 0.07, 0.10, 0.15, 0.20, 0.25, 0.30 ] ,
            "max_depth"        : [3, 4, 5, 6, 7, 8, 9, 10],
            "min_child_weight" : [ 1, 3, 5, 7, 10 ]}
    elif pType == 'Multi_Class' :
        params={'gamma':[i/10.0 for i in range(0,5)],
            'subsample':[i/10.0 for i in range(6,10)],
            'colsample_bytree':[i/10.0 for i in range(6,10)],
            'reg_alpha':[0, 0.001, 0.005, 0.01, 0.05],
            "learning_rate"    : [0.01, 0.03, 0.07, 0.10, 0.15, 0.20, 0.25, 0.30 ] ,
            "max_depth"        : [3, 4, 5, 6, 7, 8, 9, 10],
            "min_child_weight" : [ 1, 3, 5, 7, 10 ],
            "multi": ["softmax","softprob"]}
        
            #scale_pos_weight
    return params   



           
# model training
def model_train(X_train,Y_train,MLDL_Model,params):     
    MLDL_Model.estimator.set_params(**params)
    model = MLDL_Model.estimator
    model.fit(X_train,Y_train)
    return model #MLDL_Model


def main(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):  
    
    pType = utils.fetchProblemType(correlationId)[0]
    logger = utils.logger('Get',correlationId)   
    start = time.time()
    try:
    # Fetching model parameters
        if HyperTune == 'True':
            dbconn, dbcollection = utils.open_dbconn('ME_HyperTuneVersion')
            data_jsonHT=dbcollection.find({"CorrelationId":correlationId,"HTId": HTId})
            HTParams = data_jsonHT[0].get('ModelParams')            
            modelparams = HTParams                     
            MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,'XGBoost Classifier','MLDL_Model')                        
            
    # Pulling data
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType = pType,UniId = HTId)
        data = utils.data_from_chunks(correlationId,'DE_PreProcessedData')
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Data Fetched at: '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
    #Get feature selection parameters and target
        featureParams = utils.getFeatureSelectionVariable(correlationId)
        targetVar = utils.getTargetVariable(correlationId)
#        UniqueIdentifier = utils.getUniqueIdentifier(correlationId)
        UniqueIdentifier,UIDList = utils.getUniqueIdentifier(correlationId)
        selectedColumn = utils.getSelectedColumn(featureParams["selectedFeatures"],data.columns)
        
    #ensure tarter column is not selected
        selectedColumn = list(set(selectedColumn+[targetVar]))
        #selectedColumn.append('All_Text')
        selectedColumn.remove(targetVar)
        dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        data_json1 = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()
        try:
            language = data_json1[0].get('Language').lower()
        except Exception:
            language = 'english'
        dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
        data_json = dbcollection.find({"CorrelationId":correlationId})
        problemTypeflag=data_json[0].get("NLP_Flag")
        clustering_flag = data_json[0].get("Clustering_Flag")
        trainAll_flag = data_json[0].get("AllData_Flag")
        
        if isinstance(problemTypeflag,type(None)):
            problemTypeflag=False
        if isinstance(clustering_flag,type(None)):
            clustering_flag=True
        if isinstance(trainAll_flag,type(None)):
            trainAll_flag=False
        
        if problemTypeflag and not clustering_flag:
            vectorized_df = Textclassification.clustering_optional_multithread(data['All_Text'],correlationId,language=language)
        
        if problemTypeflag and not clustering_flag:
            trainData = data[selectedColumn].join(vectorized_df)
            trainData = trainData.drop('All_Text',axis=1)
        else:
            trainData = data[selectedColumn]
        targetData = data[targetVar].tolist()
        
        
    # test train split
        utils.updQdb(correlationId,'P','20',pageInfo,userId,modelName = modelName,problemType = pType,UniId=HTId)
        
        
#        remove_uniqueid = utils.checkUniqueIdInData(correlationId, list(trainData.columns))
#        if remove_uniqueid!="":
#            trainData = trainData.drop(remove_uniqueid, axis=1)
            
        folds = int(featureParams["Kfold"])
        #if pType=='Classification' or pType=='Multi_Class':
        if trainAll_flag or folds==1:
            X_train,X_test,Y_train,Y_test = utils.train_test_split_utils(trainData,targetData,test_size=0.2,
                                                   random_state=50, stratify=featureParams["stratify"])  
            X_train = trainData.copy()
            Y_train = targetData
        else:
            X_train,X_test,Y_train,Y_test = utils.train_test_split_utils(trainData,targetData,test_size=featureParams["trainTestSplitRatio"],
                                                   random_state=50, stratify=featureParams["stratify"]) 
        
        
        try:
            if np.unique(Y_train,return_counts=True)[1].min()==1:
               min_count_tuple=np.unique(Y_train,return_index=True,return_counts=True) 
               min_index=min_count_tuple[2].argmin()
               min_value=[]
               for x in min_count_tuple:
                   min_value.append(x[min_index])
               Y_train.append(min_value[0])
               df1 = X_train.loc[np.repeat(X_train.index[min_value[1]:min_value[1]+1].values, 1)]
               X_train = X_train.append(df1,ignore_index=True)
               del df1
            X_test1 = X_test.copy()
            Y_test1 = Y_test.copy()
            X_train1 = X_train.copy()
            X_train1.reset_index(drop =True,inplace=True)
            Y_train1 = Y_train.copy()
            
            if len(targetData)>len(Y_test):
                missing_targets = set(targetData)-set(Y_test)
                for item in missing_targets:
                    index = Y_train1.index(item)
                    X_test1 = X_test1.append(X_train1.loc[index])
                    Y_test1.append(item)
                X_test = X_test1.copy()
                Y_test = Y_test1.copy()
        except Exception:
            utils.logger(logger,correlationId,'INFO',('XGB Modeling: Data balance in test issue'),str(None)) 
        
        
        _X_test = X_test.copy()
        
        if UniqueIdentifier in X_train.columns:
            X_train.drop([UniqueIdentifier],axis = 1 ,inplace =True)
        elif UniqueIdentifier+"_L" in X_train.columns:
            UniqueIdentifier = UniqueIdentifier+"_L"
            X_train.drop([UniqueIdentifier],axis = 1 ,inplace =True)
        if UniqueIdentifier in X_test.columns:
            X_test.drop([UniqueIdentifier],axis = 1 ,inplace =True)
        elif UniqueIdentifier+"_L" in X_test.columns:
            UniqueIdentifier = UniqueIdentifier+"_L"
            X_test.drop([UniqueIdentifier],axis = 1 ,inplace =True)
        
        #elif pType=='Text_Classification':
        #    X_train,X_test,Y_train,Y_test = utils.train_test_split_utils(Text_NonText_Final,targetData,test_size=featureParams["trainTestSplitRatio"],
        #                                       random_state=50, stratify=featureParams["stratify"])  
        
        if pType=='Classification' or pType=='Multi_Class' :
            Y_df=pd.DataFrame(data=Y_test,columns=[targetVar],index=X_test.index)
            En_Data=pd.concat([X_test,Y_df],axis=1)
        
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Test Train split Started at :'+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
                
        utils.updQdb(correlationId,'P','30',pageInfo,userId,modelName = modelName,problemType= pType,UniId=HTId)        
        
        utils.updQdb(correlationId,'P','40',pageInfo,userId,modelName = modelName,problemType = pType,UniId=HTId)
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Model Defination Completed, Training Started for the model at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        
        # Model training
        if HyperTune == 'True':            
            model = model_train(X_train,Y_train,MLDL_Model,HTParams)              
        else:               
            model = xg.XGBClassifier()
         # Random search Cross Validate Model
            folds = int(featureParams["Kfold"])
            if pType=='Classification':
                for x in Y_train:
                    if folds<=Y_train.count(x):
                        folds=folds
                    elif Y_train.count(x)<10:
                        folds=Y_train.count(x)
                        if folds==1:
                            folds=2
                    else:
                        folds=folds
                        
            n_iter = 3
            if pType == 'Classification' :
                scoring= 'roc_auc'
            elif pType == 'Multi_Class' :
                scoring = 'accuracy'
                
            n_jobs= 1
            params = xgb_RS_params(correlationId)
            if pType=='Classification' or pType=='Multi_Class':
                model = Train_Cross_Validate.random_search_CV(model,X_train,Y_train,params,folds,
                                                          n_iter,scoring,n_jobs,counter=3)
            #if pType=='Text_Classification':
            #     model = Train_Cross_Validate.random_search_CV(model,X_train,Y_train,params,folds,
            #                                              n_iter,scoring,n_jobs,counter=3)
                
            modelparams = model.best_params_
            
        utils.updQdb(correlationId,'P','60',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Model training completed at :'+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))        
        #print (model)
        
        #Saving model to shared folder
        if HyperTune == 'True': 
            if pType=='Classification' or pType=='Multi_Class':                
                utils.save_file(model,modelName,'classification' if pType=='Classification' else pType,correlationId,pageInfo,userId,list(X_train.columns),'MLDL_Model',HTId,version=version)        
            #elif pType=='Text_Classification':
            #    utils.save_file(model,modelName,'classification' if pType=='Classification' else pType,correlationId,pageInfo,userId,FileType = 'MLDL_Model',HTId=HTId)        
        else:
            if pType=='Classification' or pType=='Multi_Class':            
                utils.save_file(model,modelName,'classification' if pType=='Classification' else pType,correlationId,pageInfo,userId,list(X_train.columns),'MLDL_Model',HTId=None,version=version)         
            #elif pType=='Text_Classification':
            #    utils.save_file(model,modelName,'classification' if pType=='Classification' else pType,correlationId,pageInfo,userId,FileType = 'MLDL_Model')        
        utils.updQdb(correlationId,'P','70',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Trained model saved at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))        
        
        ### visualization changes
        #Filtering a dataset based on UIDs
        #print (X_test.columns) 
        #Visualization changes###########
        if UIDList:            
            #ps_data = pd.DataFrame(ps_data)            
            #filtered_delta_data = []
            if not data[data[UniqueIdentifier].isin(UIDList)].empty:
                if 'All_Text' in data.columns:
                    data = data.drop('All_Text',axis=1)
                _X_test =  data[data[UniqueIdentifier].isin(UIDList)]
                #print (_X_test.columns)
                Y_test = _X_test[targetVar].tolist()
                #print (_X_test.columns)
                X_test = _X_test.drop([UniqueIdentifier,targetVar],axis = 1)
                X_test = X_test[X_train.columns]
                if pType=='Classification' or pType=='Multi_Class' :
                    Y_df=pd.DataFrame(data=Y_test,columns=[targetVar],index=X_test.index)
                    En_Data=pd.concat([X_test,Y_df],axis=1)
        # model predictions
        test_pred = model.predict(X_test)
        test_prob = model.predict_proba(X_test)   
                        
                
        utils.updQdb(correlationId,'P','80',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)    
        utils.logger(logger,correlationId,'INFO',('XGB Modeling :Predictions done at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        #print ("23141",_X_test.shape)
        #print ("hereerer",len(test_prob.tolist()))
        #print ("skdff",X_test.shape)
                
         # model evaluation
        if pType=='Classification' or pType=='Multi_Class' : 
            unique_target=utils.decode_only_target_vals(En_Data,correlationId,unique=True)
            unique_target = [utils.removespecialchar(i) for i in unique_target]
        # changes for visualization
        visualization = {}
        #print (_X_test)
        #print ("here")
        visualization["ylabelname"] = "Probability"
        if UniqueIdentifier in _X_test.columns:
            visualization["xlabel"] = _X_test[UniqueIdentifier].tolist()
            visualization["xlabelname"] = UniqueIdentifier
        else:
            visualization["xlabel"] = ["ID_"+str(n) for n in range(len(test_prob.tolist()))]
            visualization["xlabelname"] = "UniqueIdentifier"
        visualization["predictionproba"] = test_prob.tolist()
        visualization["legend"] = unique_target
        visualization["target"] = targetVar.rstrip("_L")
        #unique_target.append('maybe')
    
                
         # model evaluation
        if pType == 'Classification' :
            accuracy,log_loss,TP,TN,FP,FN,sensitivity,specificity,precision,recall,f1score,c_error,ar_score,aucEncoded = M_Evaluation.Eval_Metrics(Y_test,test_pred,test_prob,model,X_test,correlationId,pageInfo,userId)
        elif pType == 'Multi_Class':
            confusion,ConfusionEncoded,accuracy_score,matthews_coefficient,report=MultiClass_Evaluation.main(Y_test,test_pred,unique_target)
            FP = confusion.sum(axis=0) - np.diag(confusion)
            FN = confusion.sum(axis=1) - np.diag(confusion)
            TP = np.diag(confusion)
            TN = confusion.sum() - (FP + FN + TP)
        #elif pType=='Text_Classification' and len(np.unique(targetData))>2:
        #    accuracy_score,matthews_coefficient=MultiClass_Evaluation.main_Text_Classification(Y_test,list(test_pred))
            
            ##insert Multi-class metrics file here.
            ### need to insert code here ###  
       
        
        
        
        # *******Update Evaluation Metrics in DB******#
        
        utils.updQdb(correlationId,'P','90',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Model Evaluation completed at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        
        # Model Feature Importance
        if HyperTune == 'True': 
            estimator=model
        else:
            estimator=model.best_estimator_
        
        if pType == 'Classification' or pType=='Multi_Class':
            if problemTypeflag and not clustering_flag:
                featureImportance=None
            else:
                if UniqueIdentifier in trainData.columns:
                    trainData.drop([UniqueIdentifier],axis = 1 ,inplace =True)
                elif UniqueIdentifier+"_L" in trainData.columns:
                    UniqueIdentifier = UniqueIdentifier+"_L"
                    trainData.drop([UniqueIdentifier],axis = 1 ,inplace =True)
                featureImportance = Eli5_M.eli5_m(estimator,trainData,'classification' if pType=='Classification' else pType) 
        end = time.time()
                
        # *******Update ExplainableAI in DB******#
        if HyperTune == 'True':
            if pType=='Classification':
                utils.insert_EvalMetrics_FI_C(correlationId,modelName,pType,accuracy,log_loss,TP,TN,FP,FN,sensitivity,specificity,precision,recall,f1score,c_error,ar_score,aucEncoded,featureImportance,end-start,pageInfo,userId,modelparams,visualization,version=version,HTId=HTId,counter=3,clustering_flag= clustering_flag)
            elif pType=='Multi_Class':
                utils.insert_MultiClassMetrics_C(correlationId,modelName,pType,matthews_coefficient,report,TP,TN,FP,FN,ConfusionEncoded,accuracy_score,featureImportance,end-start,pageInfo,userId,modelparams,visualization,version=version,HTId=HTId,counter=3,clustering_flag= clustering_flag)
           
        else:
             if pType=='Classification' :
                 utils.insert_EvalMetrics_FI_C(correlationId,modelName,pType,accuracy,log_loss,TP,TN,FP,FN,sensitivity,specificity,precision,recall,f1score,c_error,ar_score,aucEncoded,featureImportance,end-start,pageInfo,userId,visualization,modelparams,version=version,counter=3,clustering_flag= clustering_flag)
             elif pType=='Multi_Class':
                 utils.insert_MultiClassMetrics_C(correlationId,modelName,pType,matthews_coefficient,report,TP,TN,FP,FN,ConfusionEncoded,accuracy_score,featureImportance,end-start,pageInfo,userId,visualization,modelparams,version=version,counter=3,clustering_flag= clustering_flag)
            
            
        #utils.insInputSample(correlationId,modelName,trainData.head(2))
        utils.updQdb(correlationId,'P','95',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Model Feature Importance processed at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
                
        utils.updQdb(correlationId,'C','100',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)            
    except (Exception,ArithmeticError) as e:        
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
    else:
        utils.logger(logger,correlationId,'INFO',('XGB Modeling : Model Engineering completed at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        utils.save_Py_Logs(logger,correlationId)       
    
#main('df0c7839-784e-4de6-9876-d9e6da27d660',"XGB","test",1,HyperTune=None)
    
#main("b8fb9b6c-3c40-4204-aac8-0be951aab586","SVM","test",1,HyperTune=None)
'''
if __name__ == '__main__':
    main(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None)
'''
'''
start1=56
end1=440
targetData[start1:end1] = [1]*(end1-start1)
Y_test[start1:end1] = [1]*(end1-start1)
test_pred[start1:end1] = [1]*(end1-start1)'''
