import time
start=time.time()                         
import json# -*- coding: utf-8 -*-
"""
Created on Fri Mar 27 11:47:12 2020

@author: aakhya.singh
"""
import sys
import re
import numpy as np
import requests
import configparser, os
import sys
from pathlib import Path
import platform

mainDirectory = str(Path(__file__).parent.parent.absolute())
if platform.system() == 'Linux':
    configpath = mainDirectory+"/pythonconfig.ini"
    EntityConfigpath = mainDirectory+"/pheonixentityconfig.ini"
    thai_model_path='fasttext_model/cc.th.100.bin'
elif platform.system() == 'Windows':
    configpath = mainDirectory+"\\pythonconfig.ini"
    EntityConfigpath = mainDirectory+"\\pheonixentityconfig.ini"
    thai_model_path='fasttext_model\\cc.th.100.bin'

import multiprocessing 
import concurrent.futures
from joblib import parallel_backend
from joblib import Parallel, delayed
from datetime import datetime, timedelta
#Adding reference to Root path (File encrypter)
rootPath = str(Path(__file__).parent.parent.absolute())

sys.path.insert(0, rootPath )

import file_encryptor
config = configparser.RawConfigParser()
#configpath = mainDirectory+"/pythonconfig.ini"
#EntityConfigpath = mainDirectory+"/pheonixentityconfig.ini"
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
EntityConfig = configparser.RawConfigParser()
EntityConfig.read(EntityConfigpath)	

from pathlib import Path
mainDirectory = str(Path(__file__).parent.parent.parent.absolute())
currentDirectory = str(Path(__file__).parent.absolute())
sys.path.insert(0, mainDirectory)
sys.path.insert(1, currentDirectory)
import utils
import EncryptData
from io import StringIO
import base64
import json
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
import glob
import shutil
import nltk
from nltk.stem import LancasterStemmer, WordNetLemmatizer
import spacy
from sklearn.feature_extraction.text import CountVectorizer
import pandas as pd
import os
import glob
import scipy
import time
import fasttext,fasttext.util
lemmer = WordNetLemmatizer()
vectorizer = TfidfVectorizer()
Countvectorizer=CountVectorizer(ngram_range=(1, 1))
import gc
from nltk.stem.snowball import SnowballStemmer
def remove_unused_model(spacy_vectors):
    #spacy_vectors["bengali"] = {"1":"2",'time' : time.time()}
    items_to_be_removed = []
    #item_default = config['config']['default_models']
    item_default="english"
    for item in spacy_vectors.keys():
        if item not in item_default:
            items_to_be_removed.append(item)
    for item in items_to_be_removed:
        print("before deleting:: ", str(spacy_vectors))
        spacy_vectors[item]['model'] = ''
        spacy_vectors[item]['time'] = ''
        del spacy_vectors[item]['model']
        del spacy_vectors[item]['time']
        del spacy_vectors[item]
        print("after deleting:: ", str(spacy_vectors))
    gc.collect()
    del gc.garbage[:]


end=time.time()               


def mapdict2id(mydict,mapping_sheet_data,url_link,unique_col):
    actual_col=str(unique_col).lower()
    for index,data in enumerate(mydict):
        if str(data[actual_col]) in mapping_sheet_data:
            temp=mapping_sheet_data[str(data[actual_col])]
            mydict[index][url_link]=temp
            temp_dict=mydict[index]
            temp_dict[unique_col] = temp_dict.pop(actual_col)
            mydict[index]=temp_dict
    return mydict
# func to preprocess text
def preprocess_txt(text):
    new_text = re.sub(r'[^a-zA-Z0-9\n\.]', ' ', text)
    new_text = new_text.lower()
    
    return new_text

# func to convert dataframe to txt  
def df_to_txt(dframe, cols,customFlag):
    
    if not customFlag:
        dframe.columns=dframe.columns.str.lower()
    dframe = dframe.replace('\n',' ', regex=True)
    s = StringIO()
    np.savetxt(s, dframe[cols].values, fmt='%s', delimiter =' tdelt ')
    string_tmp = s.getvalue()
    string_tmp=trainprocess_txt(string_tmp)
    return string_tmp

def trainprocess_txt(text):
    text=text.splitlines()
    final_text=''
    for data in text:
        new_text = data.lower()
        new_text=str(new_text).encode('ascii',"ignore").decode('ascii')
        #new_text=re.sub('[@!#$%^&*()<>[\]?/\|\'\"\`}{~:=;\+,-]',' ',new_text)
        #new_text = nltk.word_tokenize(new_text)
        #new_text=' '.join(new_text)
        final_text+=new_text+'\n'
    final_text=final_text[0:-1]
    return final_text

def mask_with_in1d(df,column_name,filter_data_names):
    mask = np.in1d(df[column_name].values, filter_data_names)
    return df[mask]

def vectorize(sentence,language,correlation_id,uniId):
    logger = utils.logger('Get', correlation_id)
    
    if language in ["chinese","japanese",'thai']:
        pass
    else:
        word_list = nltk.word_tokenize(sentence,language=str(language))
    if language in ['german','spanish','portugese']:
        lemmatized_output = ' '.join([stemmer.stem(w) for w in word_list]) #no lemma for non -english
        sentence = lemmatized_output
    elif language in ['french','english']:
        word_list = ' '.join([stemmer.stem(w) for w in word_list])
        word_list=nltk.word_tokenize(word_list,language=str(language))
        
        lemmatized_output = ' '.join([lemmer.lemmatize(w) for w in word_list])
        sentence = lemmatized_output
    else:
        sentence=sentence
    try:
        """
        spacy vector generation block
        """
        Countvectorizer.fit_transform([sentence])
        tokens = Countvectorizer.get_feature_names()
        
        if language=='thai':
            sent_vecs = [spacy_vectors.get_word_vector(word) for word in tokens]
        else:
            sent_vecs = [spacy_vectors(word).vector for word in tokens]
        
        temp_sum = np.sum(sent_vecs, axis=0)
        #spacy_entire_data.append(temp_sum)
        #mynewdata.append(sentence)
        return sentence,temp_sum
        """
        vector generation using spacy completes here
        """
    except Exception as e:
        utils.logger(logger,correlation_id,'INFO','****Exception from pickleGeneration#288\t'+str(e),str(uniId))
        
def pickleGeneration_multithread(language,content,correlation_id,uniId,svectors):
    try:
        logger = utils.logger('Get', correlation_id)
        language=str(language).lower()
        #if language=='french':
        #    language='english'
        utils.logger(logger,correlation_id,'INFO','****Inside pickleGeneration_training_#229 with language selected --->'+str(language),str(uniId))
        global spacy_vectors
        spacy_vectors=svectors[language]['model']
        global stemmer
        if language not in ['chinese','thai','japanese']:
           stemmer=SnowballStemmer(language)
        if language =='thai':
            spacy_stopwords = set()
        else:
            spacy_stopwords = spacy_vectors.Defaults.stop_words
            
        # if language =='spanish':
        #     spacy_vectors = spacy.load('es_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("spanish")
        # elif language =='portuguese':
        #     spacy_vectors = spacy.load('pt_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("portuguese")
        # elif language =='german':
        #     spacy_vectors = spacy.load('de_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer = SnowballStemmer("german")
        # elif language=='japanese':
        #     spacy_vectors = spacy.load('ja_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        # elif language=='chinese':
        #     spacy_vectors = spacy.load('zh_core_web_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        # elif language=='french':
        #     spacy_vectors= spacy.load('fr_core_news_lg',disable=['tagger','parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("french")
        # elif language =='thai':
        #     spacy_vectors = fasttext.load_model(thai_model_path)
        #     spacy_stopwords = set()
        # else:
        #     spacy_vectors = spacy.load('en_core_web_lg', disable=['tagger', 'parser','ner','lemmatizer'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer = SnowballStemmer("english")
        
         
        list_sentences = content.splitlines()
        utils.logger(logger,correlation_id,'INFO','After Spacy loading of Vectors',str(uniId))
        #sentence=re.sub(r'[@_!#$%^&* ()<>[\]?/\|\'\"\`}{~:;+-=.,]',r' ',sentence)
        
        mynewdata=[]
        spacy_entire_data=[]
        utils.logger(logger,correlation_id,'INFO','Training is started',str(uniId))
        if platform.system() == 'Linux':
            mynewdata,spacy_entire_data = zip(*(Parallel(n_jobs=3,backend='multiprocessing')(
                delayed(vectorize)(sentence,language,correlation_id,uniId) for sentence in list_sentences)))
        elif platform.system() == 'Windows':
            mynewdata,spacy_entire_data = zip(*(Parallel(n_jobs=3,backend='threading')(
                delayed(vectorize)(sentence,language,correlation_id,uniId) for sentence in list_sentences)))
        
            
        utils.logger(logger,correlation_id,'INFO','Training is ended for spacy',str(uniId))
        utils.logger(logger,correlation_id,'INFO','Vector generation is complete for spacy',str(uniId))
        #print('********mynewdata change',mynewdata)
        mynewdata=list(mynewdata)
        
        #mynewdata = list(filter(None, mynewdata))
        trsfm=vectorizer.fit_transform(mynewdata)
        utils.logger(logger,correlation_id,'INFO','vector generation is complete for tfidf',str(uniId))
        
        return vectorizer,spacy_entire_data,trsfm,'Fine'
    except Exception as e:
        utils.logger(logger,correlation_id,'ERROR','Trace',str(uniId))
        return 'Error',str(e),'Error','Error'


def train_similarity(correlation_id,pageInfo,uniId,svectors):
    start_time=time.time()
    logger = utils.logger('Get', correlation_id)
    try:
        model_flag=False
        cpu,memory=utils.Memorycpu()
        utils.logger(logger, correlation_id, 'INFO', ('import in train_similarity func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))                                                                                                                                                                      
        EnDeRequired = True
        CustomFlag=False
        utils.logger(logger,correlation_id,'INFO','****Inside train_similarity',str(uniId))
        # getting dataframe
        offlineutility = utils.checkofflineutility(correlation_id)
        if offlineutility:
            utils.logger(logger,correlation_id,'INFO','****Inside offlineutility_train_similarity',str(uniId))                                                                                                  
            dframe = utils.data_from_chunks_offline_utility(correlation_id, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            utils.logger(logger,correlation_id,'INFO','****Inside else of offlineutility_train_similarity',str(uniId))                                                                                                          
            dframe = utils.data_from_chunks(correlation_id, collection="AIServiceIngestData")
        
        utils.logger(logger,correlation_id,'INFO',str(len(dframe)),str(uniId))   
        
        if (len(dframe)<19):
            return False,"No. of records less than or equal to 19. Please validate the data."
        
        utils.logger(logger,correlation_id,'INFO',"322,Data types are "+str(dframe.dtypes),str(uniId)) 
        
        numerics = ['int16', 'int32', 'int64', 'float16', 'float32', 'float64']
        rem_num_cols = []

        for i in range(0,len(dframe.dtypes)):
            if str(dframe.dtypes[i]) in numerics:
                rem_num_cols.append(dframe.columns[i].lower())
        
        utils.logger(logger,correlation_id,'INFO',"Removable Numerical Columns "+str(rem_num_cols),str(uniId))
        dframe.fillna(value=np.NaN,inplace=True)
        dframe.replace('None', np.NaN, inplace=True)
        dframe.replace("NaN", np.NaN, inplace=True)
        dframe.dropna(how='all',inplace=True)
        #dframe.dropna(thresh=len(dframe.columns)-1,inplace=True)
        
        
        
        for column in dframe.columns:
            dframe[column].fillna("  ", inplace=True)

        utils.logger(logger,correlation_id,'INFO',str(dframe),str(uniId))
        utils.logger(logger,correlation_id,'INFO',str(dframe.tail(1)),str(uniId))

        model_name,model_path = utils.GetModelSimilarityName(correlation_id)
        utils.logger(logger,correlation_id,'INFO',model_name,str(uniId))                                                                                                          
        utils.logger(logger,correlation_id,'INFO',model_path,str(uniId))                                                                                                          
        
        
        usecase_id = utils.getUseCaseId(correlation_id,uniId)
        
        
        if str(usecase_id)!= config['TOUseCaseId']['DefectUseCaseId'] and str(usecase_id)!= config['TOUseCaseId']['TestCaseUseCaseId']:
        
            dbconn, dbcollection=utils.open_dbconn("AICore_Preprocessing")
            data_json = list(dbcollection.find({"CorrelationId": correlation_id}))
            
            if EnDeRequired:
                t = base64.b64decode(data_json[0].get('Filters'))
                data_json[0]['Filters'] =  eval(EncryptData.DescryptIt(t))  
                
                utils.logger(logger,correlation_id,'INFO',str(data_json[0]),str(uniId))

            if bool(data_json[0].get('Filters')):
                filters=data_json[0].get('Filters')
                for key,value in filters.items():
                    values_t = [key1 for key1,value1 in value.items() if value1=='True']            
                    #print(key,values_t)
                    if len(values_t): 
                        dframe= mask_with_in1d(dframe,key,values_t)
                        #print(dframe.shape)
            utils.logger(logger,correlation_id,'INFO',"After applying filters "+str(dframe.shape),str(uniId))
            
            custom_details,cols=utils.customRequestParamsForTraining(correlation_id,uniId)
            
        else:

            for col in list(dframe.columns):
                dframe.rename(columns={col:col.lower()},inplace=True)


            if str(usecase_id) == config['TOUseCaseId']['DefectUseCaseId']:
                
                mydict_mapping_sheet = dict(zip(dframe.externalid, dframe.externalurl))
                cols = ['title','description']

                # if len(dframe.columns.intersection(cols)) < 2:
                #     return False,"Required columns are not present in data"
 
            elif str(usecase_id) == config['TOUseCaseId']['TestCaseUseCaseId']:
                
                mydict_mapping_sheet = dict(zip(dframe.uniqueid, dframe.testexternalurl))
                cols = ['title','description','teststeptitle','teststepdescription','expectedresult']
    
            custom_details,_=utils.customRequestParamsForTO(correlation_id,uniId)

                        
        utils.logger(logger,correlation_id,'INFO','****getRequestParams',str(uniId))
        # getting col names from db
        utils.logger(logger,correlation_id,'INFO',str(correlation_id)+str(pageInfo)+str(uniId))    
        #_, cols = utils.getRequestParams(correlation_id,pageInfo,uniId)
        #custom_details, cols = utils.getRequestParams(correlation_id,'',uniId)
        
     
        #custom_details,cols=utils.customRequestParamsForTraining(correlation_id,uniId)
        uniqueId,args = utils.GetUniqueColfromDB(correlation_id,uniId)
        
        try:
            rem_num_cols.remove(uniqueId.lower())
        except Exception as e:
            utils.logger(logger,correlation_id,'INFO',"getting error wwhile removing uniqueId"+str(e.args[0]),str(uniId))
        
        #determine unique percentage and fail if less<100
        utils.logger(logger,correlation_id,'INFO',('checking unique percent in {}'.format(uniqueId)),str(uniId))
        if uniqueId==None or uniqueId=="Null" or uniqueId=="":
            uniqueId = {}
        else:
            uniqueId=uniqueId.lower()
        utils.logger(logger,correlation_id,'INFO',('Unique id selection is {}'.format(uniqueId)),str(uniId))
        
        for col in list(dframe.columns):
            dframe.rename(columns={col:col.lower()},inplace=True)
    
        utils.logger(logger,correlation_id,'INFO',"Data types are "+str(dframe.dtypes),str(uniId)) 
        
        if custom_details=='null':
            cols=[data.lower() for data in cols]#newaddition
        else:
            cols=[data.lower() for data in cols]#newaddition
            CustomFlag=True
        
        #if str(usecase_id)!=config['TOUseCaseId']['TestCaseUseCaseId']:                                                                                                   
        if(len(uniqueId)>0):
            uniquePercent = len(dframe[uniqueId].unique())*1.0/len(dframe[uniqueId])*100
            if int(uniquePercent)!=100:
                utils.logger(logger,correlation_id,'INFO',('unique percent is  {} which is less than 100 for {}, hence training will not proceed.'.format(str(uniquePercent),uniqueId)),str(uniId))
                return False,"Selected Unique Identifier has less than 100% unique values. Please select a unique identifier field with 100% unique values to proceed with Training the model"   
        utils.logger(logger,correlation_id,'INFO',uniqueId,str(uniId))
        
        utils.logger(logger,correlation_id,'INFO',('args----> {}'.format(args)),str(uniId))   
                                                                     
        
        cols.sort()
        
        if set(cols).issubset(set(rem_num_cols)):
            utils.logger(logger,correlation_id,'ERROR',"Entered the validation for numerical columns and condition is true",str(uniId))
            return False,"Text data is required to run Similarity Algorithm"  

        utils.logger(logger,correlation_id,'INFO','**********custom_details'+str(custom_details),str(uniId))

        utils.logger(logger,correlation_id,'INFO','****cols.sort()',str(uniId))
        utils.logger(logger,correlation_id,'INFO',('*****cols.sort {}'.format(cols)),str(uniId))
        
        # dframe.drop_duplicates(keep='first',inplace=True)
        
        utils.logger(logger,correlation_id,'INFO',str(dframe.columns),str(uniId))
        
        if(len(uniqueId)>0):                 
            unique_col = dframe[uniqueId.lower()]
            try:
                unique_col = unique_col.round(0).astype(int)
            except Exception as e:
                utils.logger(logger,correlation_id,'INFO',"getting error unique_col type conversion"+str(e.args[0]),str(uniId))
        elif(len(uniqueId)==0):
            unique_col = pd.Series([])
            
        utils.logger(logger,correlation_id,'INFO',"Unique data columns is " +str(len(unique_col)),str(uniId))
        
        dframe = dframe.apply(lambda x: x.astype(str).str.lower())        
        
        training_df = pd.DataFrame()
        
        utils.logger(logger,correlation_id,'INFO',"Before Stop words --- 445",str(uniId))
        
        stop_words = utils.GetStopWordsfromDB(correlation_id,uniId)  

        if stop_words==None or stop_words=="Null" or stop_words=="":
            stop_words = []
            
        utils.logger(logger,correlation_id,'INFO',('stop_words {}'.format(stop_words)),str(uniId))                               
        utils.logger(logger,correlation_id,'INFO',('dframe columns {}'.format(dframe.columns)),str(uniId))
        
        if (len(stop_words)>0):
            stop_words = [x.lower() for x in stop_words]
            pattern = '|'.join(stop_words)
            for col in cols:
                training_df[col] = dframe[col].str.replace(pattern, '')                
            utils.logger(logger,correlation_id,'INFO','Stop words are removed')
            
        elif (len(stop_words)==0):
            training_df = dframe[cols]
            utils.logger(logger,correlation_id,'INFO','Stop words are not given',str(uniId)) 

            
        utils.logger(logger,correlation_id,'INFO',('dframe columns {}'.format(training_df.columns)),str(uniId))
   
        utils.logger(logger,correlation_id,'INFO','**Rows and columns for the dataset',str(uniId))
        utils.logger(logger,correlation_id,'INFO',str(dframe.shape),str(uniId))
        #content = df_to_txt(dframe, cols,CustomFlag)
        language=utils.GetLanguageForModel(correlation_id).lower()
        
        if language== 'thai':
            temp_df = training_df.stack().groupby(level=0).apply(' tdelt '.join)
            lis = temp_df.to_list()
            content = '\n'.join([str(elem) for elem in lis])
        else:
            content = df_to_txt(training_df,cols,CustomFlag)
            
      
        training_df = training_df.drop(columns = training_df.columns.intersection(rem_num_cols))
        
        if language== 'thai':
            temp_df = training_df.stack().groupby(level=0).apply(' tdelt '.join)
            lis = temp_df.to_list()
            training_content = '\n'.join([str(elem) for elem in lis])
        else:
            training_content = df_to_txt(training_df,list(training_df.columns),CustomFlag)
        
        

        utils.logger(logger,correlation_id,'INFO',str(training_df.head(1)),str(uniId))                                                                                                                                                                                               

        utils.logger(logger,correlation_id,'INFO','**Rows and columns for the dataset',str(uniId))
        utils.logger(logger,correlation_id,'INFO',str(training_df.shape),str(uniId))                                                                                                                                                                                               
        #rows = content.split("\n")
        training_rows = training_content.split("\n") 
        utils.logger(logger,correlation_id,'INFO',str(training_rows[0]),str(uniId))                                                                                                                                                                                               
        
                                                   
        if(len(training_rows) > 0):  
            # saving content as model
            utils.logger(logger,correlation_id,'INFO','**saving model',str(uniId))
            #utils.logger(logger,correlation_id,'INFO',content)
            utils.logger(logger,correlation_id,'INFO','******new implementation for training starts',str(uniId))
            #language=utils.GetLanguageForModel(correlation_id)
            vect,spacy_data,trsfm,status=pickleGeneration_multithread(language,training_content,correlation_id,uniId,svectors)
            utils.logger(logger,correlation_id,'INFO','******#344 pickle generation returned',str(uniId))
            if 'Error' not in status:
                list_data=[vect,spacy_data,trsfm,content,unique_col]
                list_data_name=['tf_object','spacy_vect','tf_vect','file_content','unique_column']
                model_flag=True
                pickle_list = []
                individualvectors = []
                for index,individualvector in enumerate(list_data):
                    try:
                        
                        pickle_list.append(model_name+'_'+str(list_data_name[index]))
                        individualvectors.append(individualvector)

                        utils.logger(logger,correlation_id,'INFO','Appending model file #352 for '+str(list_data_name[index]),str(uniId))

                    except Exception as e:
                        utils.logger(logger,correlation_id,'INFO','Issue in saving model for block #354 for '+str(list_data_name[index]),str(uniId))
                        utils.updateModelStatus(correlation_id,uniId,"","Error",str(e))
                        model_flag=False
                        break
                #new additions for handling externallinks
                if str(usecase_id)==config['TOUseCaseId']['DefectUseCaseId']:                    
                    pickle_list.append(model_name+'_'+'mapping_sheet')
                    individualvectors.append(mydict_mapping_sheet)
                    utils.logger(logger,correlation_id,'INFO','Appending model file #556 for mapping_sheet',str(uniId))

                elif str(usecase_id)== config['TOUseCaseId']['TestCaseUseCaseId']:                    
                    pickle_list.append(model_name+'_'+'mapping_sheet')
                    individualvectors.append(mydict_mapping_sheet)
                    utils.logger(logger,correlation_id,'INFO','saved model file #556 for mapping_sheet',str(uniId))
                
                utils.save_model(correlation_id,pickle_list,uniId,individualvectors)
                
                utils.logger(logger,correlation_id,'INFO','Saved models for training',str(uniId))

                
                ########################################################
                if str(usecase_id)== config['TOUseCaseId']['DefectUseCaseId'] or str(usecase_id)== config['TOUseCaseId']['TestCaseUseCaseId']:
                    
                    mynewdata = {
                         "CorrelationId": correlation_id,
                         "UniId":  uniId,
                         "pageInfo": 'Training',
                         "Status" : "C",
                         "Message":"Training is completed"
                         }

                    utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)

                if not model_flag:
                    print("before remove spcay",svectors)
                    remove_unused_model(svectors)
                    print("after remove spcay",svectors)
                    return False,""
                
            else:
                utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))                                                                                                             
                utils.logger(logger,correlation_id,'ERROR','Trace',str(uniId))
                utils.updateModelStatus(correlation_id,uniId,"","Error",str(spacy_data))
                print("before remove spcay",svectors)
                remove_unused_model(svectors)
                print("after remove spcay",svectors)
                return False,""
        else:
            utils.logger(logger, correlation_id, 'INFO', ('Total time in train_similarity function which is for training is  %s seconds ---'%(time.time() - start_time)),str(uniId))     
            utils.updateModelStatus(correlation_id,uniId,"","Error","Upload proper data")
        print("before remove spcay",svectors)
        remove_unused_model(svectors)
        print("after remove spcay",svectors)
        return True,""

    except Exception as e:
        utils.logger(logger, correlation_id, 'INFO', ('Total time in train_similarity function which is for training is  %s seconds ---'%(time.time() - start_time)),str(uniId))
        utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId)) 
        
        if str(usecase_id)== config['TOUseCaseId']['DefectUseCaseId'] or str(usecase_id)== config['TOUseCaseId']['TestCaseUseCaseId']:
                                
            mynewdata = {
                 "CorrelationId": correlation_id,
                 "UniId":  uniId,
                 "pageInfo": 'Training',
                 "Status" : "E",
                 "Message":"Error in the training"
                 } 
            
            utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)
        print("before remove spcay",svectors)
        remove_unused_model(svectors)
        print("after remove spcay",svectors)                                                    
        return False,""

# get cosine similairty matrix
def cos_sim(input_vectors):
    similarity = cosine_similarity(input_vectors)
    return similarity
    
def nlargestCosine_indexes(cosine_values,n_nearest=5):
        
    flatten =cosine_values.flatten()
    flatten = np.delete(flatten,-1) # last value is sentence itself it will have 1
              
    if(flatten.size>n_nearest):
        return flatten.argsort()[-n_nearest:]
    else:
        #Give only top 1
        return flatten.argsort()[-1]
        
def process_txt_evaluate(text,language,stemmer):
    new_text = re.sub(r'[^a-zA-Z0-9\n\.]', ' ', text)
    new_text = new_text.lower()
    word_list=nltk.word_tokenize(new_text)
    word_list = ' '.join([stemmer.stem(w) for w in word_list])
    word_list=nltk.word_tokenize(word_list,language=str(language))
    lemmatized_output = ' '.join([lemmer.lemmatize(w) for w in word_list])
    sentence = lemmatized_output
    return sentence


def evaluate_bulk_input(correlation_id,uniId,similarity_type,trsfm_tfidf,spacyPkl):
    logger = utils.logger('Get', correlation_id)
    if(similarity_type=="Bulk" or similarity_type=="BulkforTO"):
        if(similarity_type=="Bulk" or similarity_type=="BulkforTO"):
        
            utils.logger(logger,correlation_id,'INFO',"Inside the if block",str(uniId))

            df1=pd.DataFrame(trsfm_tfidf.toarray())
            scpy=scipy.sparse.csr_matrix(df1.values)
            spacy_values=cos_sim(spacyPkl)
            cosine_values = cosine_similarity(scpy)
            similarity_matrix = np.add(spacy_values,cosine_values)/2
        
        
        return similarity_matrix
        
def evaluate_input(svectors,customFlag,start_time,language,correlation_id,content,uniId,tfidf_vect,spacyPkl,trsfm_tfidf,similarity_type,results_count, cols, cols_evaluate,n_nearest,input_index_number=None):
    language=str(language).lower()
    spacy_vectors=svectors[language]['model']
    logger = utils.logger('Get', correlation_id)
    
    if(similarity_type=="Single"):
        
        # if language =='spanish':
        #     #spacy_vectors = spacy.load('es_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("spanish")
        # elif language =='portuguese':
        #     spacy_vectors = spacy.load('pt_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("portuguese")
        # elif language =='german':
        #     spacy_vectors = spacy.load('de_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer = SnowballStemmer("german")
        # elif language=='japanese':
        #     spacy_vectors = spacy.load('ja_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        # elif language=='chinese':
        #     spacy_vectors = spacy.load('zh_core_web_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        # elif language=='french':
        #     spacy_vectors= spacy.load('fr_core_news_lg',disable=['tagger','parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("french")
        # elif language=='thai':
        #     spacy_vectors = fasttext.load_model(thai_model_path)
        #     spacy_stopwords = set()
        # else:
        # #    spacy_vectors = spacy.load('en_core_web_lg', disable=['tagger', 'parser','ner','lemmatizer'])
        # #    spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer = SnowballStemmer("english")
        
        if language not in ['chinese','thai','japanese']:
           stemmer=SnowballStemmer(language)
        if language =='thai':
            spacy_stopwords = set()
        else:
            spacy_stopwords = spacy_vectors.Defaults.stop_words
            
        text = {}
        
        utils.logger(logger,correlation_id,'INFO',"cols_evaluate**********************************"+ str(cols_evaluate),str(uniId))

        utils.logger(logger,correlation_id,'INFO',"cols*********************************" + str(cols),str(uniId))

        
        #newlyadded
        if not customFlag:
            cols_evaluate =  {k.lower(): v for k, v in cols_evaluate.items()}
        for col in cols:
            if col in cols_evaluate:
                if cols_evaluate[col] != "":
                    text[col] = cols_evaluate[col]
        #newly added
        utils.logger(logger, correlation_id, 'INFO', ('fromevaluate_input {}'.format(str(text))),str(uniId))
        # checking for invalid input
        
        utils.logger(logger,correlation_id,'INFO',str(text),str(uniId))    
        string_check= re.compile('[@_!#$%^&* ()<>[\]?/\|\'\"\`}{~:;+-=.,]')
        utils.logger(logger,correlation_id,'INFO','*****#441from evaluate_input',str(uniId))
        flag = 0
        for col in text.keys():
            ent = text[col]
            if ent != "":
                for ch in str(ent):
                    if(string_check.search(ch) == None or (ord(ch)>=48 and ord(ch)<=57)):
                        flag = 1
        utils.logger(logger,correlation_id,'INFO',"Text is " +str(text),str(uniId))
        
        
        first_col = list(text.keys())[0] 
        input = str(text[first_col]) + ' tdelt '
        for col in text.keys():
            if col != first_col:
                if text[col] != "":
                    val=str(text[col]).encode('ascii',"ignore").decode('ascii')
                    input = input + ' tdelt ' + val
        # preprocessing input
        input = input.replace("\n"," ")
        #input = preprocess_txt(input)
        if language!='japanese' and language!='thai':
            input = process_txt_evaluate(input,language,stemmer)
        else:                                            
            input=input
            
        stop_words = utils.GetStopWordsfromDB(correlation_id,uniId) 
        
        if stop_words==None or stop_words=="Null" or stop_words=="":
            stop_words = []
            
        if (len(stop_words)>0):
            for word in stop_words:
                input = input.replace(word,'') 
        
        spacyPkl = list(spacyPkl)
        # saving original data
        # removing special characters from original data
        # evaluation
        utils.logger(logger,correlation_id,'INFO',input)
        if(len(text) > 0):
            if(flag==1):  
                Countvectorizer.fit_transform([input])
                tokens = Countvectorizer.get_feature_names()
                if language == 'thai':
                    sent_vecs = [spacy_vectors.get_word_vector(word) for word in tokens]
                else:
                    sent_vecs = [spacy_vectors(word).vector for word in tokens]
                temp_sum = np.sum(sent_vecs, axis=0)
                spacyPkl.append(temp_sum)
                utils.logger(logger,correlation_id,'INFO','*****#477 evaluate_spacy_vector is appended',str(uniId))
                df1=trsfm_tfidf.toarray()
                df2=tfidf_vect.transform([input]).toarray()
                df=pd.DataFrame(np.concatenate([df1, df2]))
                scpy=scipy.sparse.csr_matrix(df.values)
                utils.logger(logger,correlation_id,'INFO','*****#482scipy conversion to sparse matrix',str(uniId))
                spacy_values=cosine_similarity(spacyPkl)
                utils.logger(logger,correlation_id,'INFO','*****#484spacy cosine is complete',str(uniId))
                cosine_values = cosine_similarity(scpy)
                utils.logger(logger,correlation_id,'INFO','*****#486 tfidf cosine is complete',str(uniId))
                utils.logger(logger,correlation_id,'INFO',spacy_values,str(uniId))
                similarity_matrix = np.add(spacy_values,cosine_values)/2
                similarity_row = np.array(similarity_matrix[df.shape[0]-1, :])
                output_type = utils.GetOutputTypefromDB(correlation_id,uniId)
                utils.logger(logger,correlation_id,'INFO',output_type,str(uniId))  

                if output_type==None or output_type=="Null" or output_type=="":
                    output_type = {}
                
                if(len(output_type)==0):
                    indices = similarity_row.argsort()[-int(results_count)-1:-1][::-1]             
                    utils.logger(logger,correlation_id,'INFO','No threshold or top n')
                    
                elif(output_type['key']=="threshold"):
                    threshold = float(output_type['value'])*0.01                                                                                    
                    indices_all = similarity_row.argsort()[::-1][1:]  
                    spacy_all=[]
                    scores_all=[similarity_row[i] for i in indices_all]
                    list_original_all=content.splitlines()
                    top_similar_all, scores_all = [list_original_all[i] for i in indices_all],[similarity_row[i] for i in indices_all]
                    
                    scores_max_all1 = {}
                    for x in range(len(top_similar_all)): 
                        scores_max_all1[top_similar_all[x]] =  np.round(float(scores_all[x]),2)
  
                    return_value = []
                    
                    custom_details,cols=utils.customRequestParamsForTraining(correlation_id,uniId)
                    cols_org=cols.copy()
                    if custom_details=='null':
                        cols=[data.lower() for data in cols]#newaddition
                    else:
                        cols=[data.lower() for data in cols]#newaddition
                        CustomFlag=True
                    cols.sort()
                    for c in cols_org:
                        idx=cols.index(c.lower())
                        cols[idx]=c
                    
                    for k,v in scores_max_all1.items():
                        return_val_item = {}
                        for ind in range(len(cols)):
                            try:
                                # cols[ind] = cols[ind].capitalize()
                                return_val_item[cols[ind]] = k.replace('\n','').split(' tdelt ')[ind]
                            except Exception as e:
                                utils.logger(logger,correlation_id,'INFO',str(e.args[0]),str(uniId))
                        return_val_item["score"] = str(v)
                        
                        if (float(return_val_item["score"])>threshold):
                            return_value.append(return_val_item)
                    utils.logger(logger,correlation_id,'INFO',str(return_value),str(uniId))
                    if(len(return_value) == 0):
                        return_value = ["There are no similarity predictions for the threshold more than than "+str(threshold)]
                    return return_value
                    
                    
                elif(output_type['key']=="top_n"):
                    results_count = int(output_type['value'])
                    indices=similarity_row.argsort()[-int(results_count)-1:-1][::-1]  
                    utils.logger(logger,correlation_id,'INFO','*****indices is complete',str(uniId))
                                                                                         
                utils.logger(logger,correlation_id,'INFO',indices,str(uniId))
                spacyPkl=[]
                
                scores=[similarity_row[i] for i in indices]
                list_original=content.splitlines()
                
                top_similar, scores = [list_original[i] for i in indices],[similarity_row[i] for i in indices]
                
                scores_max = {}
                
                for x in range(len(top_similar)): 
                    scores_max[top_similar[x]] =  np.round(float(scores[x]),2)
                    
                return_value=[]
                
                custom_details,cols=utils.customRequestParamsForTraining(correlation_id,uniId)
                cols_org=cols.copy()
                if custom_details=='null':
                    cols=[data.lower() for data in cols]#newaddition
                else:
                    cols=[data.lower() for data in cols]#newaddition
                    CustomFlag=True
                cols.sort()
                for c in cols_org:
                    idx=cols.index(c.lower())
                    cols[idx]=c
                    
                for k,v in scores_max.items():
                    return_val_item = {}
                    for ind in range(len(cols)):
                        try:
                            # cols[ind] = cols[ind].capitalize()
                            return_val_item[cols[ind]] = k.replace('\n','').split('tdelt')[ind]
                        except Exception as e:
                            utils.logger(logger,correlation_id,'INFO',str(e.args[0]),str(uniId))
                    return_val_item["score"] = str(v)
                    if (float(return_val_item["score"])>0.0):
                        return_value.append(return_val_item)
                input_index_number=0
                utils.logger(logger,correlation_id,'INFO','****#512 return value',str(uniId))
                utils.logger(logger,correlation_id,'INFO',return_value,str(uniId))
                res_list=[]
                if input_index_number!=None:
                    res_list.append(str(input_index_number))
                for i in range(len(return_value)): 
                    if return_value[i] not in return_value[i + 1:]: 
                        res_list.append(return_value[i])
                mydata=[]
                cnt=0
                #result=5
                for data in res_list:
                    if len(data)>1:
                        flag_skip=False
                        if cnt==results_count:
                            break
                        int_cnt=0
                        for keys in data:
                            if data[keys]=='':
                                int_cnt+=1
                                #flag_skip=True
                                #break
                        
                        if int_cnt<len(data)-1:
                            mydata.append(data)
                            cnt+=1
                    elif len(data)==1:
                        mydata.append(data)
                res_list=[]
                myval={}
                for index,data in enumerate(mydata):
                    if index>=1:
                        if float(data['score'])<=0.10 and index==1:
                            #myval={}
                            for new_keys in data.keys():
                                if new_keys=='score':
                                    myval['score']=0.0
                                else:
                                    myval[new_keys]='Not found'
                if len(myval)>0:
                    mynew_data=[]
                    mynew_data.append('0')
                    mynew_data.append(myval)
                    mydata=mynew_data
                res_list=mydata
                
                #custom_cnt=0
                ##newly added###
                if customFlag:
                    mynewdata=[]
                    for data in res_list:
                        if len(data)>1:
                            #custom_cnt+=1
                            #if custom_cnt>int(results_cnt):
                            #    break
                            mynewdict={}
                            for key in data:                
                                val=data[key].replace('tdelt','')
                                val=str(val).lstrip(' ')
                                val=str(val).rstrip(' ')
                                mynewdict[key]=val
                            
                            mynewdata.append(mynewdict)
                        else:
                            mynewdata.append(data)
                    res_list=mynewdata
                    
                    utils.logger(logger,correlation_id,'INFO',"After result",str(uniId))

                    
                utils.logger(logger,correlation_id,'INFO',res_list,str(uniId))
                end_time=time.time()
                utils.logger(logger,correlation_id,'INFO','*****',str(uniId))
                utils.logger(logger,correlation_id,'INFO','***total time taken for prediction in seconds',str(uniId))
                utils.logger(logger,correlation_id,'INFO','total time taken for prediction in seconds {}'.format(end_time-start_time),str(uniId))
                if(len(res_list) == 0):
                    res_list = ["No predictions for your given input. Please try with another input"]
                print("before remove spcay",svectors)
                remove_unused_model(svectors)
                print("after remove spcay",svectors)
                return res_list
            else:
                utils.updateModelStatus(correlation_id,uniId,"","Error","Please provide valid input.")
                print("before remove spcay",svectors)
                remove_unused_model(svectors)
                print("after remove spcay",svectors)
                return False

        else:
            utils.updateModelStatus(correlation_id,uniId,"","Error","Please provide valid input.")
            print("before remove spcay",svectors)
            remove_unused_model(svectors)
            print("after remove spcay",svectors)
            return False
    else:
        utils.updateModelStatus(correlation_id,uniId,"","Error","Please provide valid similarity type")
        print("before remove spcay",svectors)
        remove_unused_model(svectors)
        print("after remove spcay",svectors)
        return False        

def get_similarity(correlation_id,pageInfo,uniId,cols_evaluate,results_count,similarity_type,spacy_vectors):
    logger = utils.logger('Get', correlation_id)
    start_time=time.time()                       
    try:
        cpu,memory=utils.Memorycpu()
        utils.logger(logger, correlation_id, 'INFO', ('import in get_similarity func took' + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
        customFlag=False
        n_nearest = 5
        #newly added
        utils.logger(logger,correlation_id,'INFO',cols_evaluate,str(uniId))
        if str(results_count)=='':
            results_count=int(utils.GetNoOfResults())
        #newly added
        cols_evaluate = cols_evaluate[0]
        mappflag=False
        for key in cols_evaluate:
            pattern=re.search(r'^\s*$',cols_evaluate[key])
            if not pattern:
                mappflag=True
        if not mappflag:
            utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
            return False
        else:
        # getting model name
        #model_name = utils.GetModelName(correlation_id,uniId) old code
            model_name,model_path = utils.GetModelSimilarityName(correlation_id)
            basepath=os.path.basename(model_path)
            # file_search='_'.join(basepath.split('_')[1:3])
            # dir_name=os.path.dirname(model_path)
            # file_search_temp=file_search
            #old_file_path=dir_name+'/'+'_'+file_search_temp+'_'+str(model_name)+'.pkl'
            #try:
            #    os.remove(old_file_path)
            
            # files_list=glob.glob(dir_name+'/'+'*'+file_search+'*')
            # file_search=file_search.split('_')
            # if len(files_list)<4:
            #     utils.logger(logger,correlation_id,'INFO','#673 #models is less than 4, old approach hence re-training will be initiated',str(uniId))
            #     utils.updateModelStatus(correlation_id,file_search[1],"","Progress","Model training will be initiated since number models is lt4 using optimized approach",str(uniId))
            #     utils.updateModelStatus(correlation_id,file_search[1],"","Progress","Model training has been initiated since number models is lt4 using optimized approach",str(uniId))
            #     status=train_similarity(file_search[0],'',file_search[1])
            #     if not status:
            #         utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
            #         utils.updateModelStatus(correlation_id,file_search[1],"","Error","Model training wwith new approach has failed",str(uniId))
                    
            #         return False
            #     files_list=glob.glob(dir_name+'/'+'*'+file_search_temp+'*')
            #     if len(files_list)<4:
            #         utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
            #         utils.updateModelStatus(correlation_id,file_search[1],"","Error","Model training wwith new approach has failed",str(uniId))
            #         return False
            # elif len(files_list)==0:
            #     utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
            #     utils.updateModelStatus(correlation_id,file_search[1],"","ERROR","Trace",str(uniId))
            #     return False
            # utils.updateModelStatus(correlation_id,file_search[1],"","Completed","Model got created")
            language=utils.GetLanguageForModel(correlation_id)
            
            utils.logger(logger,correlation_id,'INFO',language,str(uniId))
            utils.logger(logger,correlation_id,'INFO','***from utils logger',str(uniId))
            utils.logger(logger,correlation_id,'INFO',model_path,str(uniId))
            #content = utils.load_model(correlation_id,model_name,uniId)
            utils.logger(logger,correlation_id,'INFO','***new change',str(uniId))
            #/mnt/myWizard-Phoenix/IngrAIn_Shared/SavedModels/_c0ca9f76-8db1-4136-adfb-9508664f9cf0_2d48a793-fb85-4eba-b0bb-eede324d26bf_v_file_content.pkl *****new one p
            
            try:
                model_paths = utils.getModelPaths(correlation_id,uniId)
                model_paths = eval(model_paths)
                
                tfidf_vect=utils.load_model_from_Path(model_paths[0])
                spacyPkl=utils.load_model_from_Path(model_paths[1])
                trsfm_tfidf=utils.load_model_from_Path(model_paths[2])
                content=utils.load_model_from_Path(model_paths[3])
                
                utils.updateModelStatus(correlation_id,uniId,"","Completed","Model got created")
                
            except:
            
                file_search='_'.join(basepath.split('_')[1:3])
                dir_name=os.path.dirname(model_path)
                file_search_temp=file_search
                
                files_list=glob.glob(dir_name+'/'+'*'+file_search+'*')
                file_search=file_search.split('_')
                
                if len(files_list)<4:
                    utils.logger(logger,correlation_id,'INFO','#673 #models is less than 4, old approach hence re-training will be initiated',str(uniId))
                    utils.updateModelStatus(correlation_id,uniId,"","Progress","Model training will be initiated since number models is lt4 using optimized approach")
                    utils.updateModelStatus(correlation_id,uniId,"","Progress","Model training has been initiated since number models is lt4 using optimized approach")
                    status=train_similarity(file_search[0],'',file_search[1])
                    if not status:
                        utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
                        utils.updateModelStatus(correlation_id,uniId,"","Error","Model training with new approach has failed")
                    
                        return False
                    files_list=glob.glob(dir_name+'/'+'*'+file_search_temp+'*')
                    if len(files_list)<4:
                        utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
                        utils.updateModelStatus(correlation_id,uniId,"","Error","Model training with new approach has failed")
                        
                        return False
                elif len(files_list)==0:
                    utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
                    utils.updateModelStatus(correlation_id,uniId,"","Error","Trace")
                    
                    return False
                    
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_file_content.pkl'

                content=utils.load_model_from_Path(model_path)
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_tf_object.pkl'
                tfidf_vect=utils.load_model_from_Path(model_path)
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_spacy_vect.pkl'
                spacyPkl=utils.load_model_from_Path(model_path)
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_tf_vect.pkl'
                trsfm_tfidf=utils.load_model_from_Path(model_path)
                
                utils.updateModelStatus(correlation_id,uniId,"","Completed","Model got created")
                
  
            #content = utils.load_model(correlation_id,model_name,uniId)
            # getting col  from db  
            utils.logger(logger, correlation_id, 'INFO', str(uniId))
            
            #_, cols = utils.getRequestParams(correlation_id,"",uniId)
            custom_details,cols=utils.customRequestParamsForTraining(correlation_id,uniId)
             
            utils.logger(logger, correlation_id, 'INFO', str(cols),str(uniId))
            cols.sort()
            if custom_details=='null':
                cols=[data.lower() for data in cols]
            else:
                customFlag=True
            # get col values for evaluation in dict 
            utils.logger(logger, correlation_id, 'INFO', str(cols_evaluate),str(uniId))
            utils.logger(logger,correlation_id, 'INFO','****from get similarity',str(uniId))
            
            utils.logger(logger,correlation_id,'INFO',str([customFlag,language,correlation_id,uniId,tfidf_vect,similarity_type,results_count, cols, cols_evaluate,n_nearest]),str(uniId))

            #final_list=[]
            if similarity_type =="Single":
                final_list=evaluate_input(spacy_vectors,customFlag,start_time,language,correlation_id,content,uniId,tfidf_vect,spacyPkl,trsfm_tfidf,similarity_type,results_count, cols, cols_evaluate,n_nearest,input_index_number=0)
            elif similarity_type=="Multiple":
                final_list=evaluate_multiple_input(spacy_vectors,customFlag,start_time,language,correlation_id,content,uniId,tfidf_vect,spacyPkl,trsfm_tfidf,similarity_type,results_count, cols, cols_evaluate,n_nearest,input_index_number=0)
            utils.logger(logger, correlation_id, 'INFO', ('Total time in get_similarity function which is for training is  %s seconds ---'%(time.time() - start_time)),str(uniId))
            return final_list                                        
    except Exception as e:
        utils.logger(logger, correlation_id, 'ERROR', 'Trace')
        return False



def get_bulk_similarity(correlation_id,pageInfo,uniId,userId,similarity_type):
    start_time=time.time()
    logger = utils.logger('Get', correlation_id)
    utils.logger(logger,correlation_id,'INFO',"Inside Bulk Similarity",str(uniId))
    try:
        cpu,memory=utils.Memorycpu()
        utils.logger(logger, correlation_id, 'INFO', ('import in get_bulk_similarity func took' + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
        
        #newly added
        #if str(results_count)=='':
            #results_count=int(utils.GetNoOfResults())
        #n_nearest = 5 #move to config
        #cols_evaluate = cols_evaluate[0]
        #input_data = utils.GetModelActualData(correlation_id,uniId)
        # getting prediction input from db
        #cols_evaluate = [{'Description':'show me'},{'Description':'please show'}]      
        # getting model name
        model_name,model_path = utils.GetModelSimilarityName(correlation_id)
        basepath=os.path.basename(model_path)
        # file_search='_'.join(basepath.split('_')[1:3])
        # dir_name=os.path.dirname(model_path)
        # file_search_temp=file_search
        
        # files_list=glob.glob(dir_name+'/'+'*'+file_search+'*')
        # file_search=file_search.split('_')
        # if len(files_list)<4:
        #     utils.logger(logger,correlation_id,'INFO','#673 #models is less than 4, old approach hence re-training will be initiated',str(uniId))
        #     utils.updateModelStatus(correlation_id,file_search[1],"","Progress","Model training will be initiated since number models is lt4 using optimized approach",str(uniId))
        #     utils.updateModelStatus(correlation_id,file_search[1],"","Progress","Model training has been initiated since number models is lt4 using optimized approach",str(uniId))
        #     status=train_similarity(file_search[0],'TrainModel',file_search[1])
        #     if not status:
        #         utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
        #         utils.updateModelStatus(correlation_id,file_search[1],"","Error","Model training wwith new approach has failed",str(uniId))
                
        #         return False
        #     files_list=glob.glob(dir_name+'/'+'*'+file_search_temp+'*')
        #     if len(files_list)<4:
        #         utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
        #         utils.updateModelStatus(correlation_id,file_search[1],"","Error","Model training wwith new approach has failed",str(uniId))
        #         return False
        # elif len(files_list)==0:
        #     utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
        #     utils.updateModelStatus(correlation_id,file_search[1],"","ERROR","No previous model files were available, please train from beginning",str(uniId))
        #     return False
        
        # utils.updateModelStatus(correlation_id,file_search[1],"","Completed","Model got created")
        language=utils.GetLanguageForModel(correlation_id)
        
        utils.logger(logger,correlation_id,'INFO','***from utils logger',str(uniId))
        utils.logger(logger,correlation_id,'INFO',str(model_path),str(uniId))
        #content = utils.load_model(correlation_id,model_name,uniId)
        utils.logger(logger,correlation_id,'INFO','***new change',str(uniId))
        
        try: 
        
            model_paths = utils.getModelPaths(correlation_id,uniId)
        
            model_paths = eval(model_paths)
        
            # model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_unique_column.pkl'
            unique_data_col=utils.load_model_from_Path(model_paths[4])
        
        except:
            
            file_search='_'.join(basepath.split('_')[1:3])
            dir_name=os.path.dirname(model_path)
            file_search_temp=file_search
        
            files_list=glob.glob(dir_name+'/'+'*'+file_search+'*')
            file_search=file_search.split('_')
            if len(files_list)<4:
                utils.logger(logger,correlation_id,'INFO','#673 #models is less than 4, old approach hence re-training will be initiated',str(uniId))
                utils.updateModelStatus(correlation_id,uniId,"","Progress","Model training will be initiated since number models is lt4 using optimized approach")
                utils.updateModelStatus(correlation_id,uniId,"","Progress","Model training has been initiated since number models is lt4 using optimized approach")
                status=train_similarity(file_search[0],'TrainModel',file_search[1])
                if not status:
                    utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
                    utils.updateModelStatus(correlation_id,uniId,"","Error","Model training wwith new approach has failed")
                    pred_out=False
                    #return False
                files_list=glob.glob(dir_name+'/'+'*'+file_search_temp+'*')
                if len(files_list)<4:
                    utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
                    utils.updateModelStatus(correlation_id,uniId,"","Error","Model training wwith new approach has failed")
                    pred_out=False
                    #return False
            elif len(files_list)==0:
                utils.logger(logger, correlation_id, 'ERROR', 'Trace',str(uniId))
                utils.updateModelStatus(correlation_id,uniId,"","Error","No previous model files were available, please train from beginning")
                #return False
                pred_out=False
            
            utils.updateModelStatus(correlation_id,uniId,"","Completed","Model got created")
            model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_unique_column.pkl'
            unique_data_col=utils.load_model_from_Path(model_path)
        
        utils.logger(logger,correlation_id,'INFO',"Unique data column is " +str(len(unique_data_col)),str(uniId))
        
        utils.logger(logger,correlation_id,'INFO',str(len(unique_data_col)),str(uniId))
         
        if not unique_data_col.empty and unique_data_col.is_unique:
            
            utils.logger(logger,correlation_id,'INFO','Unique Id is there',str(uniId))
            
            try:

                tfidf_vect=utils.load_model_from_Path(model_paths[0])
                spacyPkl=utils.load_model_from_Path(model_paths[1])
                trsfm_tfidf=utils.load_model_from_Path(model_paths[2])
                content=utils.load_model_from_Path(model_paths[3])
                
                utils.updateModelStatus(correlation_id,uniId,"","Completed","Model got created")
                
            except:
            
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_file_content.pkl'
                content=utils.load_model_from_Path(model_path)
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_tf_object.pkl'
                tfidf_vect=utils.load_model_from_Path(model_path)
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_spacy_vect.pkl'
                spacyPkl=utils.load_model_from_Path(model_path)
                model_path=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_tf_vect.pkl'
                trsfm_tfidf=utils.load_model_from_Path(model_path)            
                
            
                
                      
            customFlag = False
            
            matrix=evaluate_bulk_input(correlation_id,uniId,similarity_type,trsfm_tfidf,spacyPkl)
            
            
            utils.logger(logger,correlation_id,'INFO',"Length of Matrix is " + str(len(matrix)),str(uniId))
            #utils.logger(logger,correlation_id,'INFO',str(unique_data_col))
            utils.logger(logger,correlation_id,'INFO',str(len(unique_data_col)),str(uniId))
            sim = pd.DataFrame(data=matrix,index=unique_data_col,columns=unique_data_col)
            
            print(sim)
            
            utils.logger(logger,correlation_id,'INFO',"Head of similarity matrix "+str(sim.head()),str(uniId))

            
            output_type = utils.GetOutputTypefromDB(correlation_id,uniId)
            
            utils.logger(logger,correlation_id,'INFO',str(output_type),str(uniId))
            


            if output_type==None or output_type=="Null" or output_type=="":
                    output_type = {}
            
            
            if (similarity_type=="bulk" or similarity_type=="Bulk"):
            
                output_json = {}
                utils.logger(logger,correlation_id,'INFO',"Inside Bulk-if class",str(uniId))

                uniqueId,_ = utils.GetUniqueColfromDB(correlation_id,uniId)

                
                if len(output_type) == 0:
                    for key,values in sim.iterrows():
                        temp_dict =  values.to_dict()
                        
                        output = {k: v for k, v in sorted(temp_dict.items(), key=lambda item: item[1],reverse=True)}
            
                        output_json[key]= output
                        
                elif output_type["key"]=="threshold":   
                    threshold = float(output_type["value"]) * 0.01
                    
                    utils.logger(logger,correlation_id,'INFO',"Inside threshold if class",str(uniId))

                    for key,values in sim.iterrows():
                        temp_dict =  values.to_dict()
                        
                        output = {k: round(v,2) for k, v in sorted(temp_dict.items(), key=lambda item: item[1],reverse=True)}
                        
                        output = ({str(k):v for (k,v) in output.items() if v > threshold and str(k)!=str(key)})
                        
                        output_json[key]= output
                    
                elif output_type["key"]=="top_n":
                    
                    for key,values in sim.iterrows():
                        temp_dict =  values.to_dict()
                        
                        output = ({str(k):v for (k,v) in temp_dict.items() if str(k)!=str(key)})
                        
                        output = {k: round(v,2) for k, v in sorted(output.items(), key=lambda item: item[1],reverse=True)[:int(output_type["value"])]}
                        output_json[key]= output
                        
                data = pd.DataFrame.from_dict(output_json,orient='index')
                
                # file1 = open("myfile.txt","a")
                # file1.write(str(output_json))
                # file1.close()
                
                data.columns = data.columns.astype(str)
                
                data.reset_index(level=0, inplace=True)

                
                chunk,size = utils.files_split(data,Incremental = False,appname=None)
                utils.logger(logger,correlation_id,'INFO',"Length of chunk is "+str(len(chunk)),str(uniId))

                 
                utils.save_data_chunk_bulk(chunk, "AIServicesPrediction", correlation_id,  uniId,userId, similarity_type,uniqueId,pageInfo = 'Prediction File', Incremental=False,requestId=None,sourceDetails=None, colunivals=None, timeseries=None, datapre=None, lastDateDict=None,previousLastDate = None,DataSetUId=None)
                utils.logger(logger,correlation_id,'INFO',"Saved the chunks in DB",str(uniId))

                
                utils.logger(logger,correlation_id,'INFO','Saved the data in chunks',str(uniId))
                
                end_time=time.time()
                utils.logger(logger,correlation_id,'INFO',('Total time in bulk Prediction function which is for prediction is  %s seconds {}'.format(end_time - start_time)),str(uniId))
                utils.logger(logger,correlation_id,'INFO',str(end_time - start_time),str(uniId))
                utils.logger(logger,correlation_id,'INFO','******total time taken',str(uniId))
                
                #return True
                pred_out=True

            elif (similarity_type=="BulkforTO"):
                usecase_id = utils.getUseCaseId(correlation_id,uniId)
                utils.updPredStatus(correlation_id, 'P', '50%', 'Prediction Status', userId,uniId)
                
                try:
                
                    if len(model_paths[5])==0:
                        mynewdata = {
                        "CorrelationId": correlation_id,
                        "UniId":  uniId,
                        "PageInfo": "Prediction",
                        "Status" : "E",
                        "Message":"Since mapping_sheet_pickle is not created, prediction cant proceed, kindly perform a fresh training!!!"
                        }

                        utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)
                        
                    else:
                                                         
                        mapping_sheet_data=utils.load_model_from_Path(model_paths[5])
                    
                        
                except:
                
                    mapping_sheet_file_name=dir_name+'/'+'_'+file_search[0]+'_'+file_search[1]+'_'+model_name+'_mapping_sheet.pkl'
                    
                    if not os.path.isfile(mapping_sheet_file_name):
                    
                        mynewdata = {
                        "CorrelationId": correlation_id,
                        "UniId":  uniId,
                        "PageInfo": "Prediction",
                        "Status" : "E",
                        "Message":"Since mapping_sheet_pickle is not created, prediction cant proceed, kindly perform a fresh training!!!"
                        }

                        utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)
                    
                    mapping_sheet_data=utils.load_model_from_Path(mapping_sheet_file_name) 

                
                utils.logger(logger,correlation_id,'INFO','Length of mapping sheet data is '+str(len(mapping_sheet_data)),str(uniId))

                utils.logger(logger,correlation_id,'INFO',"Inside Bulk For TO similarity",str(uniId))

                output_json = []
                    
                uniqueId,_ = utils.GetUniqueColfromDB(correlation_id,uniId)
                
                sim = sim.round(2)
                
                utils.logger(logger,correlation_id,'INFO',"Unique identifier:"+str(uniqueId),str(uniId))

                
                if (len(output_type)==0):
                    for key,values in sim.iterrows():
                        values.rename('score',inplace=True) 
                        temp_dict = pd.DataFrame(values).reset_index()
                        temp_dict = temp_dict.sort_values(by=['score'],ascending=False).iloc[:5].to_dict(orient='records')

                        output = {}
                        output[uniqueId] = key
                        output['Predictions'] = temp_dict
                        output_json.append(output)
                
                elif output_type["key"]=="threshold" or output_type["key"]=="Threshold":   
                    threshold = float(output_type['value'])
                    
                    utils.logger(logger,correlation_id,'INFO',"Threshold is " + str(threshold),str(uniId))
                    for key,values in sim.iterrows():
                        values.rename('score',inplace=True) 
                        temp_dict = pd.DataFrame(values).reset_index()
                        temp_dict = temp_dict[temp_dict['score']>threshold].to_dict(orient='records')
                        #print('***temp_dict',temp_dict)
                        if str(usecase_id)== config['TOUseCaseId']['DefectUseCaseId']:
                            #and str(usecase_id)!= config['TOUseCaseId']['TestCaseUseCaseId']
                            temp_dict=mapdict2id(temp_dict,mapping_sheet_data,'ExternalURL','ExternalID')
                            output={}
                            output['ExternalID'] = key
                            output['Predictions'] = temp_dict
                        else:
                            temp_dict=mapdict2id(temp_dict,mapping_sheet_data,'TestExternalURL','UniqueID')
                            output={}
                            output['UniqueID'] = key
                            output['Predictions'] = temp_dict
                        #output = {}
                        #output[uniqueId] = key
                        #output['Predictions'] = temp_dict
                        #print('***Predictions****',output)
                        output_json.append(output)
                    
                elif output_type["key"]=="top_n" or output_type["key"]=="Top_n":
                    for key,values in sim.iterrows():
                        values.rename('score',inplace=True) 
                        temp_dict = pd.DataFrame(values).reset_index()
                        temp_dict = temp_dict.sort_values(by=['score'],ascending=False).iloc[:int(output_type["value"])].to_dict(orient='records')
                        if str(usecase_id)== config['TOUseCaseId']['DefectUseCaseId']:
                            #and str(usecase_id)!= config['TOUseCaseId']['TestCaseUseCaseId']
                            temp_dict=mapdict2id(temp_dict,mapping_sheet_data,'ExternalURL','ExternalID')
                            output={}
                            output['ExternalID'] = key
                            output['Predictions'] = temp_dict
                        else:
                            temp_dict=mapdict2id(temp_dict,mapping_sheet_data,'TestExternalURL','UniqueID')
                            output={}
                            output['UniqueID'] = key
                            output['Predictions'] = temp_dict
                        output_json.append(output)
                
                if (len(output_json)!=0):
                    #utils.logger(logger,correlation_id,'INFO',str(output_json))
                    utils.logger(logger,correlation_id,'INFO',"Dim of dataframe is "+str(pd.DataFrame(output_json).head()),str(uniId))

                    chunk,size = utils.files_split(pd.DataFrame(output_json),Incremental = False,appname=None)

                    utils.logger(logger,correlation_id,'INFO',"Length of chunk is " +str(len(chunk)),str(uniId))
                    statusMessage = "Success"
                    
                elif (len(output_json)==0):
                    
                    output = {}
                    output['Predictions'] = ""
                    output_json.append(output)
                    chunk = [pd.DataFrame(output_json)]
                    statusMessage = "No Prediction for given threshold/top_n values"
                    
                    
                shape = len(unique_data_col)
                
                dframe = utils.data_from_chunks(correlation_id, collection="AIServiceIngestData")
                
                if str(usecase_id) == config['TOUseCaseId']['DefectUseCaseId']:
                    
                    shape = len(dframe['ExternalID'].unique())
     
                elif str(usecase_id) == config['TOUseCaseId']['TestCaseUseCaseId']:
                    
                    shape = len(dframe['TestExternalID'].unique())
                
                utils.save_data_chunk(chunk, "AIServicesPrediction", correlation_id,  uniId, userId,similarity_type,shape,uniqueId,statusMessage,pageInfo = 'Prediction File', Incremental=False,requestId=None,sourceDetails=None, colunivals=None, timeseries=None, datapre=None, lastDateDict=None,previousLastDate = None,DataSetUId=None)
                            
                utils.logger(logger,correlation_id,'INFO','Saved the data in chunks',str(uniId))
                utils.updPredStatus(correlation_id, 'C', '100%', 'Prediction Status', userId,uniId)
                
                # output = utils.data_from_chunk(correlation_id, collection = "AIServicesPrediction", lime=None, recent=None)
                
                # utils.logger(logger,correlation_id,'INFO',output)
                
                mynewdata = {
                    "CorrelationId": correlation_id,
                    "UniId":  uniId,
                    "PageInfo": "Prediction",
                    "Status" : "C",
                    "Message":"Prediction Completed"
                    }
                
                utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)

                end_time=time.time()
                utils.logger(logger,correlation_id,'INFO',('Total time in bulk Prediction function which is for prediction is  %s seconds {}'.format(end_time - start_time)),str(uniId))
                utils.logger(logger,correlation_id,'INFO',str(end_time - start_time),str(uniId))
                utils.logger(logger,correlation_id,'INFO','******total time taken',str(uniId))
                
                #return True
                pred_out=True
        
        elif not unique_data_col.is_unique:
            
            utils.logger(logger,correlation_id,'INFO','Inside when non-unique series is given',str(uniId))

            
            if (similarity_type=="bulkforTO"):                
                mynewdata = {
                    "CorrelationId": correlation_id,
                    "UniId":  uniId,
                    "PageInfo": "Prediction",
                    "Status" : "E",
                    "Message":"Prediction is not completed because Unique ID is not unique"
                    }
                
                utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)
             
            
            end_time=time.time()
            utils.logger(logger,correlation_id,'INFO','Prediction not successful because of non unique',str(uniId))

            utils.logger(logger,correlation_id,'INFO',('Total time in bulk Prediction function which is for prediction is  %s seconds {}'.format(end_time - start_time)),str(uniId))
            utils.logger(logger,correlation_id,'INFO',str(end_time - start_time),str(uniId))
            utils.logger(logger,correlation_id,'INFO','******total time taken',str(uniId))
            
            pred_out=False
            #return False

        elif (unique_data_col.empty):
            
            utils.logger(logger,correlation_id,'INFO',"Inside when Empty series is given",str(uniId))
            
            
            if (similarity_type=="bulkforTO"):                
                mynewdata = {
                    "CorrelationId": correlation_id,
                    "UniId":  uniId,
                    "PageInfo": "Prediction",
                    "Status" : "E",
                    "Message":"Prediction is not completed because Unique ID selection is mandatory for Bulk Prediction"
                    }
                
                utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)
            
            end_time=time.time()
            utils.logger(logger,correlation_id,'INFO',('Total time in bulk Prediction function which is for prediction is  %s seconds {}'.format(end_time - start_time)),str(uniId))
            utils.logger(logger,correlation_id,'INFO',str(end_time - start_time),str(uniId))
            utils.logger(logger,correlation_id,'INFO','******total time taken',str(uniId))
            print("Break because of no unique columns")
            #return False
            pred_out=False
        #pred_queue.put(pred_out)  
        if pred_out==True:
            utils.updPredStatus(correlation_id,'C', '100%', 'Prediction Status', userId,uniId)
        else:
            utils.updPredStatus(correlation_id,'E', '', 'Prediction Status', userId,uniId)          
    except Exception as ex:
        utils.logger(logger, correlation_id, 'ERROR', 'Trace')
        
        if (similarity_type=="bulkforTO"):  
            
            mynewdata = {
                "CorrelationId": correlation_id,
                "UniId":  uniId,
                "PageInfo": "Prediction",
                "Status" : "E",
                "Message":"Error in prediction"
                }
               
            utils.callNotificationAPIforTO(correlation_id,uniId,mynewdata)
            
        utils.UpdateAIserviceCollection(correlation_id,uniId,"",'Not able to complete the prediction for bulk',str(ex))                                                                                      
        #return False
        pred_out=False
        utils.updPredStatus(correlation_id,'E', '', 'Prediction Status', userId,uniId)          
        #pred_queue.put(pred_out)            
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           


def evaluate_multiple_input(svectors,customFlag,start_time,language,correlation_id,content,uniId,tfidf_vect,spacyPkl,trsfm_tfidf,similarity_type,results_count, cols, cols_evaluate,n_nearest,input_index_number=None):
    language=str(language).lower()
    spacy_vectors=svectors[language]['model']
    logger = utils.logger('Get', correlation_id)
    
    if(similarity_type=="Multiple" ):
        print(language)
        
        # if language =='spanish':
        #     spacy_vectors = spacy.load('es_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("spanish")
        # elif language =='portuguese':
        #     spacy_vectors = spacy.load('pt_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("portuguese")
        # elif language =='german':
        #     spacy_vectors = spacy.load('de_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer = SnowballStemmer("german")
        # elif language=='japanese':
        #     spacy_vectors = spacy.load('ja_core_news_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        # elif language=='chinese':
        #     spacy_vectors = spacy.load('zh_core_web_lg',disable=['tagger', 'parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        # elif language=='french':
        #     spacy_vectors= spacy.load('fr_core_news_lg',disable=['tagger','parser'])
        #     spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer=SnowballStemmer("french")
        # elif language=='thai':
        #     spacy_vectors = fasttext.load_model(thai_model_path)
        #     spacy_stopwords = set()
        # else:
        # #    spacy_vectors = spacy.load('en_core_web_lg', disable=['tagger', 'parser','ner','lemmatizer'])
        # #    spacy_stopwords = spacy_vectors.Defaults.stop_words
        #     stemmer = SnowballStemmer("english")
        
        if language not in ['chinese','thai','japanese']:
           stemmer=SnowballStemmer(language)
        if language =='thai':
            spacy_stopwords = set()
        else:
            spacy_stopwords = spacy_vectors.Defaults.stop_words
            
        text = {}
        
        utils.logger(logger,correlation_id,'INFO',"cols_evaluate**********************************"+ str(cols_evaluate),str(uniId))

        utils.logger(logger,correlation_id,'INFO',"cols*********************************" + str(cols),str(uniId))

        
        #newlyadded
        if not customFlag:
            cols_evaluate =  {k.lower(): v for k, v in cols_evaluate.items()}
        for col in cols:
            if col in cols_evaluate:
                if cols_evaluate[col] != "":
                    text[col] = cols_evaluate[col]
        #newly added
        utils.logger(logger, correlation_id, 'INFO', ('fromevaluate_input {}'.format(str(text))),str(uniId))
        # checking for invalid input
        
        utils.logger(logger,correlation_id,'INFO',str(text),str(uniId))    
        string_check= re.compile('[@_!#$%^&* ()<>[\]?/\|\'\"\`}{~:;+-=.,]')
        utils.logger(logger,correlation_id,'INFO','*****#441from evaluate_input',str(uniId))
        flag = 0
        for col in text.keys():
            ent = text[col]
            if ent != "":
                for ch in str(ent):
                    if(string_check.search(ch) == None or (ord(ch)>=48 and ord(ch)<=57)):
                        flag = 1
        utils.logger(logger,correlation_id,'INFO',"Text is " +str(text),str(uniId))
        
        
        first_col = list(text.keys())[0] 
        input = str(text[first_col]) + ' tdelt '
        for col in text.keys():
            if col != first_col:
                if text[col] != "":
                    val=str(text[col]).encode('ascii',"ignore").decode('ascii')
                    input = input + ' tdelt ' + val
        # preprocessing input
        input = input.replace("\n"," ")
        #input = preprocess_txt(input)
        if language!='japanese' and language!='thai':
            input = process_txt_evaluate(input,language,stemmer)
        else:                                            
            input=input
            
        stop_words = utils.GetStopWordsfromDB(correlation_id,uniId) 
        
        if stop_words==None or stop_words=="Null" or stop_words=="":
            stop_words = []
            
        if (len(stop_words)>0):
            for word in stop_words:
                input = input.replace(word,'') 
        
        spacyPkl = list(spacyPkl)
        # saving original data
        # removing special characters from original data
        # evaluation
        utils.logger(logger,correlation_id,'INFO',input)
        if(len(text) > 0):
            if(flag==1):  
                Countvectorizer.fit_transform([input])
                tokens = Countvectorizer.get_feature_names()
                if language == 'thai':
                    sent_vecs = [spacy_vectors.get_word_vector(word) for word in tokens]
                else:
                    sent_vecs = [spacy_vectors(word).vector for word in tokens]
                temp_sum = np.sum(sent_vecs, axis=0)
                spacyPkl.append(temp_sum)
                utils.logger(logger,correlation_id,'INFO','*****#477 evaluate_spacy_vector is appended',str(uniId))
                df1=trsfm_tfidf.toarray()
                df2=tfidf_vect.transform([input]).toarray()
                df=pd.DataFrame(np.concatenate([df1, df2]))
                scpy=scipy.sparse.csr_matrix(df.values)
                utils.logger(logger,correlation_id,'INFO','*****#482scipy conversion to sparse matrix',str(uniId))
                spacy_values=cosine_similarity(spacyPkl)
                utils.logger(logger,correlation_id,'INFO','*****#484spacy cosine is complete',str(uniId))
                cosine_values = cosine_similarity(scpy)
                utils.logger(logger,correlation_id,'INFO','*****#486 tfidf cosine is complete',str(uniId))
                utils.logger(logger,correlation_id,'INFO',spacy_values,str(uniId))
                similarity_matrix = np.add(spacy_values,cosine_values)/2
                similarity_row = np.array(similarity_matrix[df.shape[0]-1, :])
                output_type = utils.GetOutputTypefromDB(correlation_id,uniId)
                utils.logger(logger,correlation_id,'INFO',output_type,str(uniId))  

                if output_type==None or output_type=="Null" or output_type=="":
                    output_type = {}
                
                if(len(output_type)==0):
                    indices = similarity_row.argsort()[-int(results_count)-1:-1][::-1]             
                    utils.logger(logger,correlation_id,'INFO','No threshold or top n')
                    
                elif(output_type['key']=="threshold"):
                    threshold = float(output_type['value'])*0.01                                                                                    
                    indices_all = similarity_row.argsort()[::-1][1:]  
                    spacy_all=[]
                    scores_all=[similarity_row[i] for i in indices_all]
                    
                    scores_max_all1 = {}
                        
                    for x in range(len(indices_all)): 
                        scores_max_all1[indices_all[x]] =  np.round(float(scores_all[x]),2)
                     
  
                    return_value = []
                    
                    for k,v in scores_max_all1.items():
                        return_val_item = {}
                         
                        return_val_item["id"] = str(k)

                        return_val_item["score"] = str(v)
                        
                        if (float(return_val_item["score"])>threshold):
                            return_value.append(return_val_item)
                    utils.logger(logger,correlation_id,'INFO',str(return_value),str(uniId))
                    if(len(return_value) == 0):
                        return_value = ["There are no similarity predictions for the threshold more than than "+str(threshold)]
                    return return_value
                    
                    
                elif(output_type['key']=="top_n"):
                    results_count = int(output_type['value'])
                    indices=similarity_row.argsort()[-int(results_count)-1:-1][::-1]  
                    utils.logger(logger,correlation_id,'INFO','*****indices is complete',str(uniId))
                                                                                         
                utils.logger(logger,correlation_id,'INFO',indices,str(uniId))
                spacyPkl=[]
                
                scores_all=[similarity_row[i] for i in indices]
                
                scores_max = {}
                
                for x in range(len(indices)): 
                        scores_max[indices[x]] =  np.round(float(scores_all[x]),2)
                    
                return_value=[]
                    
                for k,v in scores_max.items():
                    return_val_item = {}
                    return_val_item["id"]=str(k)
                    return_val_item["score"] = str(v)
                    if (float(return_val_item["score"])>0.0):
                        return_value.append(return_val_item)
                input_index_number=0
                utils.logger(logger,correlation_id,'INFO','****#512 return value',str(uniId))
                utils.logger(logger,correlation_id,'INFO',return_value,str(uniId))
                res_list=return_value
                    
                utils.logger(logger,correlation_id,'INFO',res_list,str(uniId))
                end_time=time.time()
                utils.logger(logger,correlation_id,'INFO','*****',str(uniId))
                utils.logger(logger,correlation_id,'INFO','***total time taken for prediction in seconds',str(uniId))
                utils.logger(logger,correlation_id,'INFO','total time taken for prediction in seconds {}'.format(end_time-start_time),str(uniId))
                if(len(res_list) == 0):
                    res_list = ["No predictions for your given input. Please try with another input"]
                print("before remove spcay",svectors)
                remove_unused_model(svectors)
                print("after remove spcay",svectors)
                return res_list
            else:
                utils.updateModelStatus(correlation_id,uniId,"","Error","Please provide valid input.")
                print("before remove spcay",svectors)
                remove_unused_model(svectors)
                print("after remove spcay",svectors)
                return False

        else:
            utils.updateModelStatus(correlation_id,uniId,"","Error","Please provide valid input.")
            print("before remove spcay",svectors)
            remove_unused_model(svectors)
            print("after remove spcay",svectors)
            return False
    else:
        utils.updateModelStatus(correlation_id,uniId,"","Error","Please provide valid similarity type")
        print("before remove spcay",svectors)
        remove_unused_model(svectors)
        print("after remove spcay",svectors)
        return False        

