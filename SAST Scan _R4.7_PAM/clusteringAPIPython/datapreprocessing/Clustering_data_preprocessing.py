# -*- coding: utf-8 -*-
"""
Created on Tue Jun  9 09:37:11 2020

@author: saurav.b.mondal
"""

import time
from SSAIutils import utils
from datapreprocessing import DataEncoding
from datapreprocessing import Textclassification
import sys
import pandas as pd
import numpy as np
import spacy
import base64
import json
from main import EncryptData
from bson import json_util

def fetchProblemType(correlationId):
    dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
    data_json = dbcollection.find({"CorrelationId" :correlationId})    
    dbconn.close()
    problemtype = data_json[0].get('Target_ProblemType')
    if problemtype == '1':
        problemtypef = 'Regression'
    elif problemtype == '2':
        problemtypef = 'Classification'
    elif problemtype == '3':
        problemtypef = 'Multi_Class'
    elif problemtype == '4':
        problemtypef = 'TimeSeries'
    elif problemtype == '5':
        problemtypef = 'Clustering'
    return problemtypef

'''def id_generator(size=20, chars=string.ascii_uppercase + string.digits):
    return ''.join(random.choice(chars) for _ in range(size))
id_generator()

data=np.array([id_generator() for i in range(2*num_rows)]).reshape(-1,2)
data=pd.DataFrame(data,columns=['Text_1','Text_2'])
'''
'''
correlationId='6962f6b6-b00a-4d20-8e70-b626659efd44'
pageInfo='DataPreprocessing'
userId='abc@1234'
'''
def str_bool(s):
     if s=='True':
        return True
     if s=='False':
        return False
     else:
          raise ValueError
          
def main(correlationId,pageInfo,userId,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,EnDeRequired=None):
    try:
        start=time.time()
        logger = utils.logger('Get',correlationId) 
        if EnDeRequired == None or not isinstance(EnDeRequired,bool):
            raise Exception("Encryption Flag is a mandatory field")
        timeSeries = False                       
        
        utils.updQdb(correlationId,'P','10',pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)  
        utils.logger(logger,correlationId,'INFO',('\n'+'Data Preprocessing'+'\'n'+"Process initiated for correlation Id :"+str(correlationId)))
        
        dbconn, dbcollection = utils.open_dbconn("Clustering_IngestData")
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        stop_words=data_json[0].get('StopWords')

        try:
            language = data_json[0].get('Language').lower() #'german'
            #if language=='french':
            #    language='english'
        except Exception:
            language = 'english'

        pos = True

        
        if language == 'english':
            spacy_vectors = spacy.load('en_core_web_lg')
        elif language == 'spanish':
            spacy_vectors = spacy.load('es_core_news_lg')
        elif language == 'portuguese':
            spacy_vectors = spacy.load('pt_core_news_lg')
        elif language == 'german':
            spacy_vectors = spacy.load('de_core_news_lg')
        elif language=='japanese':
            spacy_vectors = spacy.load('ja_core_news_lg')
        elif language=='chinese':
            spacy_vectors = spacy.load('zh_core_web_lg')
        elif language =='french':
                spacy_vectors = spacy.load('fr_core_news_lg')
        elif language=='thai':
                spacy_vectors = []
            
        
        problem_type = 'Clustering'
        clustering_type=data_json[0]['ProblemType']['Clustering']
        col_selectedbyuser=list(data_json[0].get('Columnsselectedbyuser'))
        
        if len(col_selectedbyuser)==0:
            utils.updQdb(correlationId,'E',"Columns selected by user are empty, hence can't proceed",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)   
            raise Exception("Columns selected by user are empty, hence can't proceed")
        
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            data_F = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            data_F = utils.data_from_chunks(corid=correlationId,collection="Clustering_BusinessProblem")  
        
        data= data_F[col_selectedbyuser]
        data_cols = data.columns        
        data_dtypes = dict(data.dtypes)
        ndata_cols = data.shape[1]
        datasize = (sys.getsizeof(data)/ 1024) / 1024  
    
        dbconn,dbcollection = utils.open_dbconn("Clustering_DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        Features = features_data_json[0].get('Feature Name') 
        Features1 = features_data_json[0].get('Feature Name')
        
        if EnDeRequired :
            t = base64.b64decode(Features) #DE55......................................
            Features = json.loads(EncryptData.DescryptIt(t), object_hook=json_util.object_hook)
            Features1 = json.loads(EncryptData.DescryptIt(t), object_hook=json_util.object_hook)
        
        for x in Features1.keys():
            if x not in col_selectedbyuser:
                del Features[x]
    
        features_dtypesf = {}
        for key,value in Features.items():        
            d_type = [key1 for key1,value1 in value.get('Datatype').items() if value1 =='True']
            if d_type:    
                    features_dtypesf.update({key:d_type[0]})
            else:
                features_dtypesf.update({key:'ND'})  
                
        
        drop_cols_t=[]   
        text_cols = []
        category_cols=[]
        numerical_cols=[]
        not_defined=[]
        for key5,value5 in features_dtypesf.items():
             if not timeSeries:            
                 if value5 in ['Id','Text','datetime64[ns]']:
                        drop_cols_t.append(key5)
                 if value5 == 'Text':
                        text_cols.append(key5)
                 if value5 =='category':
                      category_cols.append(key5)
                 if value5 =='float64' or value5=='int64':
                      numerical_cols.append(key5)
                 if value5 =='Select Option':
                      not_defined.append(key5)
             if timeSeries:
                if value5 in ['Id']:
                    drop_cols_t.append(key5)
                #if problemtypef=='Text_Classification':
                #    if value5 in ['Id','datetime64[ns]']:
                #        drop_cols_t.append(key5)
        drop_cols_t= list(set(drop_cols_t)- set(text_cols))+not_defined
        
        if drop_cols_t:
            data.drop(drop_cols_t,axis=1,inplace=True)
        
        if clustering_type.get('Text')=='True':
            data=data[text_cols]
        elif clustering_type.get('Non-Text')=='True':
            data=data[list(set(data.columns)- set(text_cols))]
        
                
        if clustering_type.get('Text')=='True':
            missing_values=data.columns[data.isnull().all()].tolist()
            data_dropped_index_1=list(data.isnull().apply(lambda x: all(x), axis=1).to_numpy().nonzero())
            data.dropna(inplace=True,how='all')
            if data.shape[0] == 0:
                utils.updQdb(correlationId,'E','All rows have been dropped and Dataframe is empty',pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)            
                raise Exception('All rows have been dropped and Dataframe is empty')

        if clustering_type.get('Non-Text')=='True':
            missing_values=data.columns[data.isnull().any()].tolist() 
            data_dropped_index_1=list(pd.isnull(data).any(1).to_numpy().nonzero())
            missingvalues_dict={}
            for col in list(set(numerical_cols)&set(missing_values)):
                data[col].fillna(data[col].mean(),inplace=True)
                missingvalues_dict[col]=data[col].mean()
            for col in list(set(category_cols)&set(missing_values)):
                data[col].fillna(data[col].mode()[0],inplace=True)
                missingvalues_dict[col]=data[col].mode()[0]
            
    
       
            for i in numerical_cols:        
                    q1 = data[i].quantile(0.25)
                    q3 = data[i].quantile(0.75)
                    iqr = q3-q1 
                    data[i] = np.where(((data[i] < (q1 - 1.5 * iqr)) |(data[i] > (q3 + 1.5 * iqr))),data[i].mean(),data[i])
            
        freq_dict={}
        drop_text_cols=[]
        if clustering_type.get('Text')=='True':
            
            for col in text_cols:
                data[col] = data[col].astype(str)               
                dict_feature={}
                dict_feature['freq_most'] = {}
                #if int(text_dict.get(col).get('Most_Frequent'))!=0:
                freq_most = pd.Series(' '.join(data[col]).split()).value_counts()[:5] #remove most frequent words
                freq_most = list(freq_most.index)
                data[col]=utils.lambda_exec(data[col],freq_most)
                dict_feature['freq_most'] = freq_most
                
                dict_feature['freq_rare']= {}
                freq_rare = pd.Series(' '.join(data[col]).split()).value_counts()[-5:]  #remove least frequent words
                freq_rare = list(freq_rare.index) #list of those words
                data[col] = utils.lambda_exec(data[col],freq_rare)
                dict_feature['freq_rare'] = freq_rare
                freq_dict[col] = dict_feature
            for x in text_cols:
                data[x] = data[x].astype(str)     
                # data[x] = data[x].apply(Textclassification.text_process,args=[True,True,False,[]])
                data[x] = data[x].apply(Textclassification.text_process1,args=[language,spacy_vectors,True,True,False,stop_words])
                data.replace(r'^\s*$',np.nan,regex=True,inplace=True)
                
                
        null_values_df=data.isnull().mean()*100
        null_values_df_75=null_values_df[null_values_df>75]
        drop_text_cols=list(null_values_df_75.index.values)
        if drop_text_cols:
            data.drop(drop_text_cols,axis=1,inplace=True)
        
        null_values_df_lessthan20=null_values_df[null_values_df<20]
        if len(null_values_df_lessthan20)>0:
            temp_data_indexdrop_2 = pd.isnull(data).any(1)
            data_dropped_index_2 = list(temp_data_indexdrop_2[temp_data_indexdrop_2 == True].index)
            data.dropna(inplace=True)
        else:
            data_dropped_index_2=[]
                
                
        
        
        LEcols=[]
        if clustering_type.get('Non-Text')=='True':            
            if category_cols:
                LEcols=category_cols
                x,_,lencm,Lenc_cols = DataEncoding.Label_Encode(data[category_cols], LEcols)
                utils.save_file(lencm,'LE',Lenc_cols,correlationId,pageInfo,userId,FileType='LE')
        
        drop_cols = LEcols
        if drop_cols:
                data_t = data.drop(drop_cols,axis=1)
                data=pd.concat([data_t,x[Lenc_cols]],axis=1)
        else:
            data_t = data
        
        try:
            data_dropped_index=list(data_dropped_index_1[0])+list(data_dropped_index_2)
        except:
              data_dropped_index=list(data_dropped_index_1[0])
        data=data[~data.isin([np.nan, np.inf, -np.inf]).any(1)].reset_index(drop=True)
        if data.isnull().values.any():
            utils.logger(logger,correlationId,'INFO', ('data dtypes'+str(dict(data.isna().sum()))))
            utils.updQdb(correlationId,'E',"Their are nulls in data, preprocess data, Missing values",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)            
            raise Exception ("Their are nulls in data, preprocess data, Missing values")
        
        
        if clustering_type.get('Text')=='True' and len(text_cols)!=0:
                data.reset_index(inplace=True, drop=True)
                data['All_Text'] = data[list(set(data.columns) and set(text_cols)-set(drop_text_cols))].astype(str).apply(' '.join, axis=1)
                data.drop(list(set(data.columns) and set(list(text_cols))-set(drop_text_cols)),inplace=True,axis=1)
        if clustering_type.get('Text')=='True' and len(text_cols)==0:
            
            utils.updQdb(correlationId,'E',"The Uploaded Data does not contain any Text Columns. Hence, Text Clustering is not possible. Try choosing Clustering as Non-Text instead",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)   
            raise Exception ("The Uploaded Data does not contain any Text Columns. Hence, Text Clustering is not possible. Try choosing Clustering as Non-Text instead")
        if clustering_type.get('Text')=='True' and len(data)==len(data[data['All_Text']==''].index):
            utils.updQdb(correlationId,'E',"After TextPreprocessing, the Text Values are blank. Please either select different columns or use a different file for clustering",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)   
            raise Exception ("After TextPreprocessing, the Text Values are blank. Please either select different columns or use a different file for clustering")
        if clustering_type.get('Non-Text')=='True' and len(category_cols+numerical_cols)==0: 
             utils.updQdb(correlationId,'E',"The Uploaded Data does not contain any Non-Text Columns. Try choosing Clustering as Text instead or review your data once again",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)              
             raise Exception ("The Uploaded Data does not contain any Non-Text Columns. Try choosing Clustering as Text instead or review your data once again")
        
        ##saving the data to DB
        
        filesize = utils.save_data(correlationId,pageInfo,userId,'Clustering_DE_PreProcessedData',data=data,datapre=True)
        
        #if EnDeRequired :
        #     freq_dict = EncryptData.EncryptIt(str(freq_dict))
        
        dbconn,dbcollection = utils.open_dbconn('Clustering_DataPreprocessing')
        dbcollection.update_many({"CorrelationId"     : correlationId},
                                { "$set":{      
                                   "CorrelationId"     : correlationId,
                                   "pageInfo"          : pageInfo,
                                   "CreatedBy"         : userId,
                                   #"Text_Null_Columns_Less20":drop_text_cols,
                                   "Frequency_dict":freq_dict,
                                   "Clustering_type":clustering_type,
                                   "Dropped_cols":drop_cols_t,
                                   "Dropped_text_cols":drop_text_cols,
                                   "Text_columns":text_cols,
                                   "Final_Text_columns":list(set(text_cols)-set(drop_text_cols)),
                                   "Category_columns":category_cols,
                                   "Numerical_columns":numerical_cols,
                                   "Label_Encoded_columns":LEcols,
                                   "Stored_MissingValues":missing_values,
                                   "DCUID": "null" if DCUID==None else DCUID,
                                   "ClientID":"null" if ClientID==None else ClientID,
                                   "ServiceID":"null" if ServiceID==None else ServiceID,
                                   "data_dropped_indices":np.array(data_dropped_index).tolist()
                                  
    
                                   }},upsert=True)
        dbconn.close()
        
        utils.updQdb(correlationId, 'C', '100', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        
    except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger,correlationId,'ERROR','Trace')
        #utils.save_Py_Logs(logger,correlationId)
        raise Exception (e.args[0])        
    else:
        utils.logger(logger,correlationId,'INFO',('\n'+"Data Transformation completed for correlation Id :"+str(correlationId))) 
        utils.save_Py_Logs(logger,correlationId)       