# -*- coding: utf-8 -*-
"""
Created on Mon Apr 22 13:12:21 2019

@author: sravan.kumar.tallozu
"""

import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
from datapreprocessing import DataEncoding
from datapreprocessing import Data_Modification
from datapreprocessing import Textclassification
from datapreprocessing import WoE_Binning
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
from sklearn import preprocessing
import spacy
from SSAIutils import EncryptData #En............................
import base64
import json
import pickle
from pandas import Timestamp

'''
correlationId="a94a7dcb-00a1-44b9-8898-08388afb882c"
WFId="36b8f90f-972d-43dd-bdd7-b18509c4ff24"
pageInfo='WFTeachTest'
userId='user@1234'
bulk='False'
model='Random Forest Classifier'
'''
def str_bool(s):
     if s=='True':
        return True
     if s=='False':
        return False
     else:
          raise ValueError

def Normalizer(data):
    scaler = preprocessing.Normalizer().fit(data)
    data = scaler.transform(data)
    return data

def main(correlationId,WFId,model,pageInfo,userId,bulk=None):    
    try:        
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,UniId = WFId)
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        utils.logger(logger,correlationId,'INFO',("WF Ingested : Process initiated at : "+ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
            
        if bulk == 'True':
            data_t = utils.data_from_chunks(corid=correlationId,collection="WF_IngestedData",recent='True')         
            data = data_t.dropna(axis=0)  
            #data=data.head()
            
            data = utils.DateTimeStampParser(data)
            data_cols = list(data.columns)
        elif bulk =='False':        
            exc_cols=[]
            dbproconn,dbprocollection = utils.open_dbconn("WhatIfAnalysis")
            data_json = dbprocollection.find({"CorrelationId" :correlationId,"WFId": WFId})  
            dbproconn.close()
            time.sleep(1)
            Features = data_json[0].get('Features')
            if EnDeRequired:     
                t = base64.b64decode(Features)
                Features = eval(EncryptData.DescryptIt(t))            
            dataF = {}
            for key, value in Features.items():
                if value.get('Selection') == 'False':
                    exc_cols.append(value.get('Name'))
                dataF.update({value.get('Name'):value.get('Value')})                        
            
            data = pd.DataFrame([dataF]) 
            data = utils.DateTimeStampParser(data)                                  
            data_cols = list(data.columns)
            data_cols.sort()
            
            
            
            
        utils.updQdb(correlationId,'P','20',pageInfo,userId,UniId = WFId) 
        
        
        dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()
        time.sleep(1)
        featureParams = utils.getFeatureSelectionVariable(correlationId)
        #targetVar = utils.getTargetVariable(correlationId)
        
        target_variable = data_json[0].get('TargetColumn')
        input_columns = featureParams["selectedFeatures"]
        if target_variable in input_columns:
            input_columns.remove(target_variable)
        dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
        data_json_new = dbcollection.find({"CorrelationId":correlationId})
        new_features = []
        dbconn.close()
        #print(data_json_new[0])
        try:
            new_features = data_json_new[0].get("Features_Created") 
            value_Store = data_json_new[0].get("Add_Feature_Value_Store")
            existingFeatures = data_json_new[0].get("Existing_Features")
            features_not_created = data_json_new[0].get("Feature_Not_Created")
        except:
            new_features = []
            value_Store = []	
            existingFeatures = []
            features_not_created =[]				
        #if len(new_features) >0:
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        feature_mapping=features_data_json[0].get('NewAddFeatures')
        if feature_mapping != None and str(feature_mapping)!='' and len(feature_mapping) >=1:
            from datapreprocessing import Add_Features
            #print(type(features_not_created))
            if features_not_created != None and len(features_not_created)>0:
                for item in list(features_not_created.keys()):
                    feature_mapping.pop(item)
                       
            data,feature_not_created = Add_Features.add_new_features(data,feature_mapping,pageInfo="publishModel",value_Store = value_Store )
            if len(feature_not_created) > 0:
                raise Exception("Some derived feature was not created. {}".format(list(feature_not_created.keys())))
            else:
                features_created = list(set(list(feature_mapping.keys())) - set(feature_not_created.keys()))
                #data = data.dropna()
                input_columns = list(set(input_columns).union(set(features_created)))
                
                for key in features_created:
                    #print("key:", key)
                    #print(data[key])
                    #if key in features_created:
                    if data.dtypes[key] not in ['int64','float64']:
                        #map_encode_new_feature[key] = {'attribute': 'Nominal','encoding': 'Label Encoding','ChangeRequest': 'True','PChangeRequest': 'False'}
                        if data.dtypes[key] =='bool':
                            data[key] = data[key].astype(str)
                #text add feature related changes
                #forming the text dataframe again with added text features

        
        data_copy = data.copy()
        if target_variable in list(data_copy.columns):
             data_copy.drop(target_variable,axis=1,inplace=True)
        #input_columns = data_json[0].get('InputColumns') 
        #uniqueIdentifier = data_json[0].get('TargetUniqueIdentifier')
        #input_columns = input_columns +[uniqueIdentifier]
        #target_variable = data_json[0].get('TargetColumn')
        dbconn,dbcollection = utils.open_dbconn("ME_FeatureSelection")
        data_json = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()     
        problemTypeflag=data_json[0].get("NLP_Flag")
        clustering_flag = data_json[0].get("Clustering_Flag")
        

        if isinstance(problemTypeflag,type(None)):
            problemTypeflag=False
        if isinstance(clustering_flag,type(None)):
            clustering_flag=True
        if problemTypeflag:
            if clustering_flag:
                dbconn,dbcollection = utils.open_dbconn("ME_FeatureSelection")
                data_json = dbcollection.find({"CorrelationId" :correlationId})    
                dbconn.close()     
                text_cols=data_json[0].get("Final_Text_Columns")
                data_text_df=data[text_cols] 
                cluster_columns =data_json[0].get("Cluster_Columns")
                input_columns = list(set(input_columns)-set(cluster_columns))
            elif not clustering_flag:
                input_columns = list(set(input_columns)-set(['All_Text']))
                dbproconn,dbprocollection = utils.open_dbconn("ME_FeatureSelection")
                data_json = dbprocollection.find({"CorrelationId" :correlationId})  
                text_cols=data_json[0].get("Final_Text_Columns")
                data_text_df=data[text_cols] 
                input_columns = input_columns+text_cols
            
        data = data[input_columns]                                    
        
        dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        data_json1 = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()
        try:
            language = data_json1[0].get('Language').lower()
        except Exception:
            language = 'english'
        #Fetch user selected data types
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        time.sleep(1)
        Features = features_data_json[0].get('Feature Name')
        if EnDeRequired :
            t = base64.b64decode(Features)           #En......................................
            Features = json.loads(EncryptData.DescryptIt(t))
        try:
           dbconn,dbcollection = utils.open_dbconn('DE_DataCleanup')
           data_json = dbcollection.find({"CorrelationId" :correlationId})    
           dbconn.close()
           Features_new = features_data_json[0].get('Feature Name_New Feature')
           if EnDeRequired :
               t = base64.b64decode(Features_new)           #En......................................
               Features_new = json.loads(EncryptData.DescryptIt(t))
           Features = {**Features, **Features_new}   
        except Exception as e:
            error_encounterd = str(e.args[0])                           
        #Removing Correlated columns
        try:
            datacols_F=list(Features.keys())
            data = data[datacols_F]
            data_cols = data.columns        
#            data_dtypes = dict(data.dtypes)
#            ndata_cols = data.shape[1]
#            datasize = (sys.getsizeof(data)/ 1024) / 1024 
        except Exception as e:
            error_encounterd = str(e.args[0])
        
        utils.updQdb(correlationId,'P','30',pageInfo,userId,UniId = WFId)                                
        
        dbconn,dbcollection = utils.open_dbconn("SSAI-savedModels")
        data_savedMod = dbcollection.find({"CorrelationId" :correlationId}) 
        count = dbcollection.count_documents({"CorrelationId" :correlationId}) 
        trans = {}
        for indx in range(count):
            if data_savedMod[indx].get('FileType') == ('StandardScaler' or 'Normalizer'):            
                trans.update({data_savedMod[indx].get('FileName') :data_savedMod[indx].get('FilePath') })            
        dbconn.close()
        time.sleep(1)
        
        utils.updQdb(correlationId,'P','40',pageInfo,userId,UniId = WFId) 
        
        dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
        data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
        dbproconn.close()
        time.sleep(1)
        utils.updQdb(correlationId,'P','45',pageInfo,userId,UniId = WFId) 
        
        #Scalers 
        if  EnDeRequired :
           t = base64.b64decode(data_json[0].get('DataModification'))
           data_json[0]['DataModification']     =  eval(EncryptData.DescryptIt(t))
           
        Features = data_json[0].get('DataModification',{}).get('Features')  
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
        
        utils.updQdb(correlationId,'P','50',pageInfo,userId,UniId = WFId)     
        
        #Binning      
        binning_data= data_json[0].get('DataModification',{}).get('ColumnBinning')
        temp = {}
        if binning_data:
            #if target_variable in binning_data.keys():
            #    del binning_data[target_variable]
            
            for  keys,values in binning_data.items():                                
                if values.get('ChangeRequest',{}).get('ChangeRequest') == 'True' and (values.get('PChangeRequest',{}).get('PChangeRequest') == 'True' or values.get('PChangeRequest',{}).get('PChangeRequest') == ''):                                        
                    for keys1,values1 in values.items():  
                        if values1.get('Binning') == 'True':
                            Ncat = values1.get('NewName') 
                            subcat =  values1.get('SubCatName')
                            temp.update({subcat:Ncat})
                    Ucat = list(set(temp.values()))      
                    for indx in range(len(Ucat)):
                        column = str(keys)                                       
                        subcat = [key2 for key2,value2 in temp.items() if value2 == Ucat[indx]]    
                        if column in data.columns:
                            data.loc[data[column].isin(subcat),column]=Ucat[indx]
        
        '''AUTOBINNING CHANGES START HERE'''
        dbconn,dbcollection = utils.open_dbconn("ME_FeatureSelection")
        data_json_bin = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()     
        bins_dict = pickle.loads(eval(data_json_bin[0].get("Binning_Dict")))
        
        if bins_dict!={}:
            data = WoE_Binning.bin_data(data, bins_dict)
        '''AUTOBINNING CHANGES END HERE'''
        
        utils.updQdb(correlationId,'P','55',pageInfo,userId,UniId = WFId) 
        '''CHANGES START HERE'''
        #print("adding new features") 
        #map_encode_new_feature={}
        #features_created = []
        
        #dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
        #data_features = dbcollection.find({"CorrelationId" :correlationId}) 
        #features_created = data_features[0].get('Features_Created')
        #encode_new_feature = data_features[0].get('Map_Encode_New_Feature')
        
        #if len(features_created)>0:
        #    if len(encode_new_feature)>0:
        #        for item in encode_new_feature:
        #            map_encode_new_feature[item] = {'attribute': 'Nominal','encoding': 'Label Encoding','ChangeRequest': 'True','PChangeRequest': 'False'}
        
        
        '''TP CHANGES START '''
        text_dict= data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
        if problemTypeflag:
            text_cols_drop=text_dict.get('TextColumnsDeletedByUser')
            #data_text_df.drop(text_cols_drop,axis=1,inplace=True)
            
            
            dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
            data_features = dbcollection.find({"CorrelationId" :correlationId}) 
            features_created = data_features[0].get('Frequency_dict')
            text_cols = data_features[0].get('Final_Text_Columns')
            text_cols = list(set(text_cols)-set(text_cols_drop))
            data_text_df=data_text_df[text_cols]
            drop_text_cols = data_features[0].get('Text_Null_Columns_Less20')
            if text_cols_drop!=text_cols:
                if len(text_cols)!=0:
                        for col in text_cols:
                            data_text_df[col] = data_text_df[col].astype(str)
                for col in text_cols:
                    
                        freq_most = features_created[col]['freq_most'] #remove most frequent words
                        freq_most = list(freq_most)
                        data_text_df[col]=utils.lambda_exec(data_text_df[col],freq_most)
                    
                        freq_rare = features_created[col]['freq_rare']  #remove least frequent words
                        freq_rare = list(freq_rare) #list of those words
                        data_text_df[col] = utils.lambda_exec(data_text_df[col],freq_rare)
            
                for key5,value5 in text_dict.items():
                        if key5 not in ["Feature_Generator","N-grams","TextColumnsDeletedByUser","NumberOfCluster","Clustering","Aggregation"]:
                            for key6,value6 in value5.items():
                            #print('Key5',key6)
                            #print('value',value6)
                                if value6 in ['True','False']:
                                    text_dict[key5][key6] = str_bool(value6)
                                    
                
                
                
                for x in text_cols:
                        
                        if language =='english':
                            spacy_vectors = spacy.load('en_core_web_lg')
                        elif language =='spanish':
                            spacy_vectors = spacy.load('es_core_news_lg')
                        elif language =='portuguese':
                            spacy_vectors = spacy.load('pt_core_news_lg')
                        elif language =='german':
                            spacy_vectors = spacy.load('de_core_news_lg')
                        elif language =='chinese':
                            spacy_vectors = spacy.load('zh_core_web_lg')
                        elif language =='japanese':
                            spacy_vectors = spacy.load('ja_core_news_lg')
                        
                        data_text_df[x] = data_text_df[x].apply(Textclassification.text_process1,args=[True,text_dict[x]['Lemmitize'],text_dict[x]['Stemming'],text_dict[x]['Stopwords'],language,spacy_vectors])
                    
                #data_text_df.replace(r'^\s*$',np.nan,regex=True,inplace=True)
                
                
                #drop_text_cols=list(null_values_df_20)
                #final_text_cols=list(set(text_cols)-set(drop_text_cols))
                    
                if drop_text_cols!=[]:
                    data_text_df.drop(drop_text_cols,axis=1,inplace=True)
        
        
        
        
        '''CHANGES END HERE'''
        #Encoding                            
        Data_to_Encode=data_json[0].get('DataEncoding')  
        '''CHANGES START HERE'''
        #if len(map_encode_new_feature)>0:
        #    Data_to_Encode.update(map_encode_new_feature)
        '''CHANGES END HERE'''
        OHEcols = []
        LEcols = []
        encoders = {}
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
        
        '''AUTOBINNING CHANGES START HERE'''
        #for the columns which  are autobinned, do not perform any kind of encoding
        if bins_dict!={}:
            autobinned_cols = [key for key in bins_dict]
            OHEcols = list(set(OHEcols) - set(autobinned_cols))
            LEcols = list(set(LEcols) - set(autobinned_cols)) 
        
        '''AUTOBINNING CHANGES END HERE'''
        
        
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
                        print(item)
                        lencm.classes_=np.append(lencm.classes_,item)
        else:
            pass
        
        
        
        utils.updQdb(correlationId,'P','60',pageInfo,userId,UniId = WFId)     
        
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
            
        utils.updQdb(correlationId,'P','65',pageInfo,userId,UniId = WFId)     
        
        if data_t.isnull().values.any():
            raise Exception("Their are nulls in data, preprocess data, Missing values")
            #utils.updQdb(correlationId,'E',"Their are nulls in data, preprocess data, Missing values",pageInfo,userId,UniId = WFId)            
            #utils.save_Py_Logs(logger,correlationId)
            #return    
            
        temp_col1 = list(data_t.columns[data_t.dtypes == 'datetime64[ns]'])
        temp_col2 = list(data_t.columns[(data_t.dtypes == 'object')])
        temp_col=temp_col1+temp_col2
        if len(temp_col) != 0:  
            data_t = data_t.drop(temp_col,axis=1)
               
        
        if ('object' or 'datetime64[ns]') in list(data_t.dtypes):
            data_tdtypes = dict(data_t.dtypes)
            utils.logger(logger,correlationId,'INFO', ('data dtypes '+str(data_tdtypes)),str(None))
            raise Exception("Their are strings/datatime stamps in data, preprocess data, Data Encoding")
            #utils.updQdb(correlationId,'E',"Their are strings/datatime stamps in data, preprocess data, Data Encoding",pageInfo,userId,UniId = WFId)                
            #utils.save_Py_Logs(logger,correlationId)
            #return   
        
        dbconn,dbcollection = utils.open_dbconn('SSAI_RecommendedTrainedModels')
        get_version_record = dbcollection.find({"CorrelationId" :correlationId,"modelName":model}) 
        version_list=[]   
        for i in range(0,get_version_record.count()):
                     version_list.append(get_version_record[i].get('Version'))
        
        if max(version_list)!=0:
            version=max(version_list)
        else:
            version=0
        MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,model,'MLDL_Model',version=version)  
        
        '''TP CHANGES START'''
        if problemTypeflag and len(data_text_df.columns)!=0:
            data_t=pd.concat([data_t,data_text_df],axis=1)
            data_t.dropna(inplace=True)
            data_t['All_Text'] = data_t[list(set(data_t.columns) and set(data_text_df.columns))].astype(str).apply(' '.join, axis=1)
            data_t.drop(list(set(data_t.columns) and set(data_text_df.columns)),inplace=True,axis=1) 
            data_t.replace(r'^\s*$',np.nan,regex=True,inplace=True)
            data_t.dropna(inplace=True)
            data_copy.drop(index=list(set(data_copy.index)-set(data_t.index)),inplace=True)
            data_t.reset_index(drop=True, inplace=True)
            data_copy.reset_index(drop=True, inplace=True)
            
        if problemTypeflag:
            '''CLUSTERING CHNAGES START'''
            if clustering_flag:
                vec=text_dict.get("Feature_Generator")
                if vec=='Count Vectorizer':
                    vectorizer=utils.load_vectorizer(correlationId,'Count Vectorization')
                if vec=='Tfidf Vectorizer':
                    vectorizer=utils.load_vectorizer(correlationId,'Tfidf Vectorizer')
                if vec=='Word2Vec':
                    vectorizer=utils.load_vectorizer(correlationId,'Word2Vec')
                if vec=='Glove':
                    vectorizer=utils.load_vectorizer(correlationId,'Glove') 
                d= utils.load_cluster_dictionary(correlationId)
                optimal_clusters = len(cluster_columns)
                
                Cluster_Dict = {}
                for clust in range(optimal_clusters):
                    Cluster_Dict['Cluster'+str(clust)] = []
            
             
                text = data_t['All_Text']
                # Assigning count to clusters
                for row_text in text:
            #         print(text.index[text == row_text])
            #         print(row_text)
            #         Cluster_Dict['Row_Text'].append(row_text)
                    try:
                        vectorizer.fit_transform(pd.Series(row_text))
                    except ValueError:
                        for k in d.keys():
                            Cluster_Dict['Cluster'+str(k)].append(0)
                    except:
                        print('Error in "Ngram" selection')
                    else:
                        for key, values in d.items():
                            cluster_sum = 0
                            for val in vectorizer.get_feature_names():
                                if val in values:
                                    cluster_sum += 1
                            Cluster_Dict['Cluster'+str(key)].append(cluster_sum)
            
             
            
                # Creating Final Cluster Dataframe
                cluster_df = pd.DataFrame.from_dict(Cluster_Dict)
                data_t = pd.concat([data_t,cluster_df],axis=1)
                data_t.drop(["All_Text"],axis=1,inplace=True)
            elif not clustering_flag:
                vectorized_df = Textclassification.clustering_optional_multithread(data_t['All_Text'],correlationId,pageInfo,language)
                data_t = data_t.join(vectorized_df)
                data_t = data_t.drop('All_Text',axis=1)
            
        '''TP CHANGES END'''
        
        if ProblemType == "classification" or ProblemType== "Multi_Class":
            if model!='SVM Classifier':
                MLDL_Model =  MLDL_Model.best_estimator_
            if model=='XGBoost Classifier':
                traincols=MLDL_Model.get_booster().feature_names
                if not clustering_flag:
                    traincols2 = [int(x) for x in traincols[-vectorized_df.shape[1]:]]
                    traincols = traincols[:-vectorized_df.shape[1]] + traincols2
            if bulk =='False':
                for coll in exc_cols:            
                    for colm in list(data_t.columns):
                        if re.search(str(coll+'_'),colm):
                            #data_t[colm] = 0
                            data_t.drop([colm],axis=1,inplace=True)
                            traincols.remove(colm)
                        if re.search(str(coll),colm):
                            #data_t[colm] = 0
                            data_t.drop([colm],axis=1,inplace=True)
                            traincols.remove(colm)
            test_data = data_t[traincols]
        elif ProblemType == "regression":
            if bulk =='False':
                for coll in exc_cols:            
                    for colm in list(data_t.columns):
                        if re.search(str(coll+'_'),colm):
                            #data_t[colm] = 0
                            data_t.drop([colm],axis=1,inplace=True)
                            traincols.remove(colm)
                        if re.search(str(coll),colm):
                            #data_t[colm] = 0
                            data_t.drop([colm],axis=1,inplace=True)
                            traincols.remove(colm)
            test_data = data_t[traincols]
#        test_data_cols = list(test_data.columns)
        
        utils.updQdb(correlationId,'P','70',pageInfo,userId,UniId = WFId)   
        
        if ProblemType == "classification" or ProblemType== "Multi_Class":
            predict_fn = lambda x: MLDL_Model.predict_proba(pd.DataFrame(x,columns=traincols)).astype(float)
        
        else:
            predict_fn = lambda x: MLDL_Model.predict(x).astype(float)           
        #if ProblemType!='Text_Classification':
        if clustering_flag:
            exp_data = utils.data_from_chunks(correlationId,'DE_PreProcessedData',lime=True)
            exp_data = exp_data[traincols]
            explainer = lime.lime_tabular.LimeTabularExplainer(exp_data.values,mode = 'classification' if ProblemType=='Multi_Class' else ProblemType,feature_names = traincols)
            
            utils.updQdb(correlationId,'P','80',pageInfo,userId,UniId = WFId)   
            time.sleep(1)
        
        if ProblemType == "classification" or ProblemType== "Multi_Class" :
            dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
            filterdata_json = dbcollection.find({"CorrelationId" :correlationId})    
            dbconn.close()
            time.sleep(1)
            target_classes = filterdata_json[0].get('UniqueTarget')
        predictions = {} 
        
        for indx,row in enumerate(test_data.iterrows()):  
            Atest_pred = 'None'
            temp_pred = {}
            temp_exp  = {}
            Data = dict(row[1])   
            
            testD = pd.DataFrame([Data],columns=traincols)
            if model =='SVM Regressor':
                testD= Normalizer(testD)
                testD=pd.DataFrame(testD,columns=traincols)
            test_pred = MLDL_Model.predict(testD)
            if LETarget == 'True':
                Atest_pred= lencm.inverse_transform(test_pred)
            else:
                Atest_pred = test_pred           
             
            try:                
                OGData = utils.get_OGData(row[1],encoders)
                Data = {}
                for ri,rj in OGData.iteritems():
                    if type(rj) == int:
                        Data.update({ri:float(rj)})
                    else:
                        Data.update({ri:rj})
            except:                
                Data = {}
                for ri,rj in row[1].iteritems():
                    if type(rj) == int:
                        Data.update({ri:float(rj)})
                    else:
                        Data.update({ri:rj})
            if problemTypeflag:
                if clustering_flag:
                    for col in cluster_columns:
                        Data.pop(col)
                elif not clustering_flag:
                    for col in list(vectorized_df.columns):
                        Data.pop(col)
                for col in text_cols:
                    Data[col] = data_copy[col][indx]
            temp_pred.update({"Data": Data})     
                
            temp_pred.update({"Prediction": Atest_pred[0]}) 
            
            if ProblemType == "classification" :
                othpred=(set(target_classes) - set(Atest_pred)).pop()   #to display against probablitites         
                temp_pred.update({"OthPred": othpred}) 
            
                
                test_prob = MLDL_Model.predict_proba(testD)
                probablitites = {"Probab1":float(max(test_prob[0])),
                               "Probab2":float(min(test_prob[0]))}
                temp_pred.update({"Probablities": probablitites})
            
            elif ProblemType== "Multi_Class" :
                 common_classes=[x for x in lencm.classes_ if x in target_classes]
                 if len(temp.keys())>0:
                     target_binnned_classes = set(target_classes).intersection(set(temp.keys()))
                     binned_target_list = []
                     if len(target_binnned_classes)>0:
                         for item,value in temp.items():
                             if item in target_binnned_classes:
                                 binned_target_list.append(value)
                     binned_target_list = list(set(binned_target_list))
                     common_classes = common_classes+binned_target_list
                 othpred=list(set(common_classes) - set(Atest_pred))
                 temp_pred.update({"OthPred": othpred}) 
                 
                 common_classes1 =[]
                 for item in MLDL_Model.classes_:
                     #print(item)
                     if lencm.inverse_transform([int(item)]) in common_classes:
                         common_classes1.append(lencm.inverse_transform([int(item)])[0])
                 common_classes = common_classes1 
                 test_prob = MLDL_Model.predict_proba(testD)
                 dict1={}
                 
                 if model=='SVM Classifier':
                     sortorder = np.argsort(MLDL_Model.decision_function(testD))[0][::-1]
                     test_prob[0].sort()	
                 for i in range(0,test_prob.shape[1]):
                     if model!='SVM Classifier':
                        x = common_classes[i]
                        x = x.replace('.', '\uff0e')
                        x = x.replace('$', '\uff04')
                        print("::::::::::::::::::::",common_classes[i])
                        dict1[x]=float(test_prob[0][i])
                     else:
                        #print (sortorder,i,sortorder[i],common_classes[sortorder[i]], dict1,test_prob[0])
                        x1 = common_classes[sortorder[i]]
                        x1 = x1.replace('.', '\uff0e')
                        x1 = x1.replace('$', '\uff04')
                        dict1[x1]=float(test_prob[0][::-1][i])
                 
                 if len(dict1)>=5:
                     dict1=dict(sorted(dict1.items(),key=lambda t : t[1] , reverse=True)[:5])
                 else:
                     dict1=dict(sorted(dict1.items(),key=lambda t : t[1] , reverse=True))
                 
                 #dict1={k:round(v,2)*100 for k,v in dict1.items()}
                 temp_pred.update({"Probablities": dict1})
            #print (testD.values[0],predict_fn)    
            
            if clustering_flag:
                exp = explainer.explain_instance(testD.values[0],predict_fn, num_features=testD.shape[1])
                exp_map = exp.as_map()
                exp_feat = exp_map.get(1)
                for feat in exp_feat:            
                    temp_exp.update({traincols[feat[0]] : feat[1]})  
               #temp_pred.update({"FeatureWeights":temp_exp})    
                temp_exp = collections.OrderedDict(sorted(temp_exp.items()))    
                FImp={}
                col_fimp = []
                if problemTypeflag:
                    data_cols = data_cols+list(cluster_df.columns)
                for col in data_cols:     
                    col_t = str(col)+'_'
                    for col1,imp in temp_exp.items():
                        if re.search(col_t,col1):
                            col_fimp.append(imp)
                        elif col == col1:
                            col_fimp.append(imp)
                         
                    FI = np.median(col_fimp) if not math.isnan(np.median(col_fimp)) else 0
                    FImp.update({col:FI}) 
                    col_fimp = []    
                FImp_f=collections.OrderedDict(sorted(FImp.items() , key=lambda t : t[1] , reverse=True))
                
                temp_pred.update({"FeatureWeights":FImp_f})
            else:
                temp_pred.update({"FeatureWeights":None})
            
            if problemTypeflag:
                if clustering_flag:
                    try:
                        word_cloud = Textclassification.wordcloud_weights(text[indx],FImp_f,d,vectorizer)
                        message = ""
                    except Exception:
                        word_cloud =""
                        message = "The Word Cloud was not generated"
                    temp_pred.update({"WordCloud":{"image":word_cloud,"message":message}})
                else:
                    temp_pred.update({"WordCloud":{"image":"","message":""}})
            else:
                temp_pred.update({"WordCloud":{"image":"","message":""}})
            predictions.update({'Prediction'+str(indx):temp_pred})
            
        utils.updQdb(correlationId,'P','90',pageInfo,userId,UniId = WFId)   
        time.sleep(1)
        if EnDeRequired:
            predictions = EncryptData.EncryptIt(json.dumps(predictions))
        dbconn, dbcollection = utils.open_dbconn("WF_TestResults")
        Id1 = str(uuid.uuid4())   
        
        
        #problem_type_flag= True
        #if ProblemType=='Text_Classification' and len(target_classes)==2:
        #    problem_type_flag= False
        #elif ProblemType=='Text_Classification' and len(target_classes)>2:
        #    problem_type_flag= True
        
        dbcollection.insert_many([{"_id" : Id1,  
                                    "CorrelationId":correlationId,
                                    "WFId": WFId,
                                    "ScenarioName":"",
                                    "Temp" :"False",
                                    "BulkTest" : bulk,
                                    "Model":model,
                                    "Target":str(target_variable),
                                    "ProblemType": ProblemType,
                                    "Predictions":predictions,
                                    "Clustering_Flag": clustering_flag,
                                    "CreatedBy":userId,
                                    "CreatedOn":str(datetime.datetime.now())
                                   # "Predicted_Value_TC": None if ProblemType!='Text_Classification' else predicted_value_TC[0],
                                    #"Predict_Probability_TC": None if ProblemType!='Text_Classification' else predict_fn_TC[0]
                                    }])
        time.sleep(1)
        dbconn.close()
        utils.updQdb(correlationId,'C','100',pageInfo,userId,UniId = WFId)  
        utils.logger(logger,correlationId,'INFO',('WFAnalysis completed sucessfully for with WFId : '+ str(WFId) +' at '+ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
              
    except Exception as e:
#        dbproconn.close()
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,UniId = WFId)
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        #utils.save_Py_Logs(logger,correlationId)
       
       
    else:
        utils.logger(logger,correlationId,'INFO',(' WF Analysis '+" Process completed successfully at "+ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        #utils.save_Py_Logs(logger,correlationId)  
       
        
    
    
      
        
    
