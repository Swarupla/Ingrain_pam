# -*- coding: utf-8 -*-
"""
Created on Tue Jul  7 15:07:39 2020

@author: s.siddappa.dinnimani
"""




#from cryptography.fernet import Fernet
##corr = "0feb4ea1-4a1c-4835-b970-35eae5b292ff"
#pickleKey = "H0MQfk330ktG24hPHVjZ_BaH4Hqxd41CUz8Zmqkd0J8="
import pickle
from SSAIutils import utils
from SSAIutils import EncryptData
import platform

if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'
    # pylogs_dir = '\IngrAIn_Python\PyLogs\\'
import configparser, os

mainPath = os.getcwd() + work_dir
import sys

sys.path.insert(0, mainPath)
sys.path.insert(0, mainPath + '/main')
import file_encryptor

config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
    
saveModelPath = config['filePath']['saveModelPath']

dbconn, dbcollection =utils.open_dbconn("SSAI_DeployedModels")
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
#print (len(flag_absent_unique))
#exit()
for corr in flag_absent_unique:
    try:
        try:
            dbconn, dbcollection = utils.open_dbconn("SSAI_savedModels")
            correlation_allsavedmodels = list(dbcollection.find({"CorrelationId":corr}))
            fileP = []
            for eachModel in correlation_allsavedmodels:
                fileP.append(eachModel['FilePath'])
            if len(fileP) <1:
                print (corr,"No File")			
                continue
            #else:
            #    print (corr)
            #    continue
            for file_path in list(set(fileP)):
                print (corr,file_path)
                pkl_file = open(file_path, 'rb')
                file = pickle.load(pkl_file)   
                utils.encryptPickle(file,file_path)
        except Exception as e:
            not_found_model = corr
            error_encountered = str(e.args[0])
        

        vec = ['Count Vectorization','Tfidf Vectorizer','Word2Vec','Glove']
        for name in vec:
            try:
                file_path = saveModelPath + '_' + corr + '_' + name + '.pkl'
                print ("vectoizer ",corr,file_path)
                pkl_file = open(file_path, 'rb')
                file = pickle.load(pkl_file)   
                utils.encryptPickle(file,file_path)
            except FileNotFoundError:
                print ("File not found")
            except Exception as e:
                print (file_path,str(r.args[0]))
                
        oth = ['cluster','RawOrginalData']
        file_path = saveModelPath + "cluster" + "_" + corr + ".pkl"
        for name in oth:
            try:
                file_path = saveModelPath + name + "_" + corr + ".pkl"
                print ("other ",corr,file_path)
                pkl_file = open(file_path, 'rb')
                file = pickle.load(pkl_file)   
                utils.encryptPickle(file,file_path)
            except FileNotFoundError:
                print ("File not found")
            except Exception as e:
                print (file_path,str(r.args[0]))
            
        dbconn, dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        dbcollection.update_many({"CorrelationId": corr},{"$set":{"DBEncryptionRequired":False}})
        dbconn.close()
        print (corr,"Done")

		
    except Exception as e:
         not_found_model = corr
         error_encountered = str(e.args[0])
    



    




        
        
