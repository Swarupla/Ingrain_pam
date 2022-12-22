# -*- coding: utf-8 -*-
"""
Created on Thu May 23 21:03:11 2019

@author: harsh.nandedkar
"""

import logging
from sklearn.ensemble import GradientBoostingRegressor
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
from datapreprocessing import Train_Cross_Validate
from evaluation import Eli5_M
import time
from evaluation import regression_evaluation
from datapreprocessing import Textclassification
from pandas import Timestamp
from datetime import datetime


def gbRegressor_RS_params():
    params={'n_estimators':[10,50,80,100,120,150,200],
			'learning_rate'    : [0.01, 0.03, 0.07, 0.10, 0.15, 0.20, 0.25, 0.30 ] ,
			'subsample':[i/10.0 for i in range(6,10)],
            'min_samples_split':[i/10.0 for i in range(2,5)],
			'min_samples_leaf':[i/10.0 for i in range(1,6)],
            'max_depth'        : [3, 4, 5, 6, 7, 8, 9, 10]}
    return params

def model_train(X_train,Y_train,MLDL_Model,params):     
    MLDL_Model.estimator.set_params(**params)
    model = MLDL_Model.estimator
    model.fit(X_train,Y_train)
    return MLDL_Model

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
            MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,'Gradient Boost Regressor','MLDL_Model')                        
            
    # Pulling data
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        data = utils.data_from_chunks(correlationId,'DE_PreProcessedData')
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Data Fetched at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
    #Get feature selection parameters and target
        featureParams = utils.getFeatureSelectionVariable(correlationId)
        targetVar = utils.getTargetVariable(correlationId)
#        UniqueIdentifier = utils.getUniqueIdentifier(correlationId)
        UniqueIdentifier,UIDList = utils.getUniqueIdentifier(correlationId)#visualization change
        selectedColumn = utils.getSelectedColumn(featureParams["selectedFeatures"],data.columns)
        
    #ensure tarter column is not selected
        selectedColumn = list(set(selectedColumn+[targetVar]))
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
        utils.updQdb(correlationId,'P','20',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        
#        remove_uniqueid = utils.checkUniqueIdInData(correlationId, list(trainData.columns))
#        if remove_uniqueid!="":
#            trainData = trainData.drop(remove_uniqueid, axis=1)
        
        if trainAll_flag:
            X_train,X_test,Y_train,Y_test = utils.train_test_split_utils(trainData,targetData,test_size=0.2,
                                                   random_state=50, stratify=featureParams["stratify"])  
            X_train = trainData.copy()
            Y_train = targetData
        else:
            X_train,X_test,Y_train,Y_test = utils.train_test_split_utils(trainData,targetData,test_size=featureParams["trainTestSplitRatio"],
                                                   random_state=50, stratify=featureParams["stratify"])        
        
        if UniqueIdentifier in X_train.columns:
            X_train.drop([UniqueIdentifier],axis = 1 ,inplace =True)
        elif UniqueIdentifier+"_L" in X_train.columns:
            UniqueIdentifier = UniqueIdentifier+"_L"
            X_train.drop([UniqueIdentifier],axis = 1 ,inplace =True)#visualization change
        _X_test = X_test.copy()
        if UniqueIdentifier in X_test.columns:
            X_test.drop([UniqueIdentifier],axis = 1 ,inplace =True)
        elif UniqueIdentifier+"_L" in X_test.columns:
            UniqueIdentifier = UniqueIdentifier+"_L"
            X_test.drop([UniqueIdentifier],axis = 1 ,inplace =True)
        
        
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor: Test Train split Started at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
                
        utils.updQdb(correlationId,'P','30',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)        
        
        utils.updQdb(correlationId,'P','40',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Model Defination Completed, Training the model started at: '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        
        # Model training
        if HyperTune == 'True':            
            model = model_train(X_train,Y_train,MLDL_Model,HTParams)            
        else:               
            model = GradientBoostingRegressor()
         # Random search Cross Validate Model
            folds = int(featureParams["Kfold"])
            n_iter = 2
            scoring= 'explained_variance'
            n_jobs= 1
            params = gbRegressor_RS_params()
            model = Train_Cross_Validate.random_search_CV(model,X_train,Y_train,params,folds,
                                                          n_iter,scoring,n_jobs)
            modelparams = model.best_params_
            
        utils.updQdb(correlationId,'P','60',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Model training completed at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))        
        #print (model)
        
        #Saving model to shared folder
        if HyperTune == 'True':                      
            utils.save_file(model,modelName,'regression',correlationId,pageInfo,userId,list(X_train.columns),'MLDL_Model',HTId,version=version)        
        else:            
            utils.save_file(model,modelName,'regression',correlationId,pageInfo,userId,list(X_train.columns),'MLDL_Model',HTId=None,version=version)        
        
        utils.updQdb(correlationId,'P','70',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Trained model saved at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))        
        
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
        
        # model predictions
        test_pred = model.predict(X_test)
        #test_prob = model.predict_proba(X_test)   
                        
                
        utils.updQdb(correlationId,'P','80',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)    
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Predictions done at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        visualization = {}
        visualization["ylabelname"] = targetVar
        if UniqueIdentifier in _X_test.columns:
            visualization["xlabel"] = _X_test[UniqueIdentifier].tolist()
            visualization["xlabelname"] = UniqueIdentifier
        else:
            visualization["xlabel"] = ["ID_"+str(n) for n in range(len(test_pred.tolist()))]
            visualization["xlabelname"] = "UniqueIdentifier"
        visualization["predictionproba"] = test_pred.tolist()
        visualization["legend"] = [targetVar]
        visualization["target"] = targetVar
         # model evaluation
        
        r2ScoreVal,rmsVal,maeVal,mseVal = regression_evaluation.evaluate_reg(Y_test,test_pred, multioutput=None)
          
        
        # *******Update Evaluation Metrics in DB******#
        
        utils.updQdb(correlationId,'P','90',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Model Evaluation completed at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        
        # Model Feature Importance
        estimator=model.best_estimator_
        if problemTypeflag and not clustering_flag:
            featureImportance=None
        else:
            if UniqueIdentifier in trainData.columns:
                trainData.drop([UniqueIdentifier],axis = 1 ,inplace =True)
            elif UniqueIdentifier+"_L" in trainData.columns:
                UniqueIdentifier = UniqueIdentifier+"_L"
                trainData.drop([UniqueIdentifier],axis = 1 ,inplace =True)
            featureImportance = Eli5_M.eli5_m(estimator,trainData,'regression') 
        end = time.time()
                
        # *******Update ExplainableAI in DB******#
        if HyperTune == 'True':
            utils.insert_EvalMetrics_FI_R(correlationId,modelName,'regression',r2ScoreVal, rmsVal, maeVal, mseVal,featureImportance,end-start,pageInfo,userId,visualization,modelparams,version=version,HTId=HTId,counter=3,clustering_flag= clustering_flag)
        else:    
            utils.insert_EvalMetrics_FI_R(correlationId,modelName,'regression',r2ScoreVal, rmsVal, maeVal, mseVal,featureImportance,end-start,pageInfo,userId,visualization,modelparams,version=version,HTId=None,counter=3,clustering_flag= clustering_flag)
            
        #utils.insInputSample(correlationId,modelName,trainData.head(2))
        utils.updQdb(correlationId,'P','95',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Model Feature Importance processed at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
                
        utils.updQdb(correlationId,'C','100',pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)        
    except (Exception,ArithmeticError) as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName = modelName,problemType=pType,UniId = HTId)
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
    else:
        utils.logger(logger,correlationId,'INFO',('Gradient Boosting Regressor : Model Engineering completed at : '+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        utils.save_Py_Logs(logger,correlationId)       