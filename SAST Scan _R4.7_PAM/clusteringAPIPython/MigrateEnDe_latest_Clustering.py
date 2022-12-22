# -*- coding: utf-8 -*-
"""
Created on Tue Jul  7 15:07:39 2020

@author: s.siddappa.dinnimani
"""



import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
#from cryptography.fernet import Fernet
##corr = "0feb4ea1-4a1c-4835-b970-35eae5b292ff"
pickleKey = "H0MQfk330ktG24hPHVjZ_BaH4Hqxd41CUz8Zmqkd0J8="
newkey="ANjSCkmQhGuEUgW5hupPY20pnN6155t4nJt5r9vzDHs="
import pickle
from SSAIutils import utils
from main import EncryptData
import platform
import pandas as pd
from cryptography.fernet import Fernet

if platform.system() == 'Linux':
    conf_path = '/main/pythonconfig.ini'
    work_dir = '/'
elif platform.system() == 'Windows':
    conf_path = '\main\pythonconfig.ini'
    work_dir = '\\'
    # pylogs_dir = '\IngrAIn_Python\PyLogs\\'
import configparser, os

mainPath = os.getcwd() + work_dir
import sys

sys.path.insert(0, mainPath)
#sys.path.insert(0, mainPath + '/main')
from main import file_encryptor

config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)

#provider, newkey, vector = file_encryptor.get_from_vault(configpath)
    
saveModelPath = config['filePath']['saveModelPath']

def encryptPickle(file,file_path):
    pickeledData = pickle.dumps(file)
    f = Fernet((bytes(newkey,"utf-8")))
    encrypted_data = f.encrypt(pickeledData)
    if platform.system() == 'Windows':
        file_path = open(bytes(file_path, "utf-8").decode("unicode_escape"), 'wb')
    else:
        file_path = open(file_path, 'wb')
    pickle.dump(encrypted_data, file_path)
    return 

def decryptPickle(filepath):
    if platform.system() == 'Windows':
        file = open(bytes(filepath, "utf-8").decode("unicode_escape"), 'rb')
    else:
        file = open(filepath, 'rb')
    loadedPickle = pickle.load(file)
    f = Fernet((bytes(pickleKey,"utf-8")))
    model = pickle.loads(f.decrypt(loadedPickle))
    return model

#corr="2c8ae903-8981-4762-9d74-c30a4358672a"
dbconn, dbcollection =utils.open_dbconn("Clustering_IngestData")
documents = list(dbcollection.find({}))
flag_absent = []
flag_present = []
flag_type = []
for item in documents:
    try:
        k = item["DBEncryptionRequired"]
        flag_type.append(k)
        flag_present.append(item["CorrelationId"])
    except:
        flag_absent.append(item["CorrelationId"])
      
flag_absent_unique = list(set(flag_absent))
#exit()
env='DevUT'
df=pd.DataFrame()
for corr in flag_present:
    try:
        try:
            dbconn, dbcollection = utils.open_dbconn("Clustering_SSAI_savedModels")
            correlation_allsavedmodels = list(dbcollection.find({}))
            fileP = []
            for eachModel in correlation_allsavedmodels:
                fileP.append(eachModel['FilePath'])
            if len(fileP) <1:
                continue
            #else:
            #    continue
            for file_path in list(set(fileP)):
                try:
                    model=decryptPickle(file_path)
                    df=df.append(pd.DataFrame([{"CorrelationId":corr,"Message":"File Present","Decryption Note":"Successful","Env":env}]))
                    encryptPickle(model,file_path)
                    df=df.append(pd.DataFrame([{"CorrelationId":corr,"Message":"File Present","Encryption Note":"Successful","Decryption Note":"Successful","Env":env,"FileType":"Model Pickle","FilePath":file_path}]))
                except Exception as e:
                    error_encounterd = str(e.args[0])
        except Exception as e:
        
            df=df.append(pd.DataFrame([{"CorrelationId":corr,"Message":"File Absent","Encryption Note":"UnSuccessful","Decryption Note":"UnSuccessful","Env":env,"FileType":"Model Pickle","FilePath":None}]))
            error_encounterd = str(e.args[0])
        
        
#        vec = ['Tfidf Vectorizer']
#        for name in vec:
#            try:
#                file_path = saveModelPath + '_' + corr + '_' + name + '.pkl'
#                try:
#                    model=decryptPickle(file_path)
#                    encryptPickle(model,file_path)
#                except:
#                    pass
#                df=df.append(pd.DataFrame([{"CorrelationId":corr,"Message":"File Present","Encryption Note":"Successful","Decryption Note":"Successful","Env":env,"FileType":"Vectorizer Pickle","FilePath":file_path}]))
#            except FileNotFoundError:
#                df=df.append(pd.DataFrame([{"CorrelationId":corr,"Message":"File Absent","Encryption Note":"UnSuccessful","Decryption Note":"Successful","Env":env,"FileType":"Vectorizer Pickle","FilePath":None}]))
#            except Exception as e:
#                df=df.append(pd.DataFrame([{"CorrelationId":corr,"Message":"File Absent","Encryption Note":"UnSuccessful","Decryption Note":"Successful","Env":env,"FileType":"Vectorizer Pickle","FilePath":None}]))
        #dbconn, dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        #dbcollection.update_many({"CorrelationId": corr},{"$set":{"DBEncryptionRequired":False}})
        #dbconn.close()
        df.to_csv("Model_Data_results_Clustering.csv")

    except Exception as e:
         error_encounterd = str(e.args[0])
        
        

    



    




        
        
