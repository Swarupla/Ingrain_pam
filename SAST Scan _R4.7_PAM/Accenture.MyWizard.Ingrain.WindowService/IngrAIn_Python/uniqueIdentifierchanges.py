# -*- coding: utf-8 -*-
"""
Created on Fri Jul 17 13:42:39 2020

@author: s.siddappa.dinnimani
"""

import platform
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'
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


from SSAIutils import utils
import uuid
from datetime import datetime

import warnings
warnings.filterwarnings("ignore")

dbconn2, dbcollection2 = utils.open_dbconn("SSAI_DeployedModels")
documents = list(dbcollection2.find({}))
dbconn2.close()
corrids = []
for item in documents:
    corrids.append(item["CorrelationId"])
    
corrids = list(set(corrids))

migrated_corrs = []
for v in corrids:
    dbconn, dbcollection = utils.open_dbconn("PS_UsecaseDefinition")
    documents = list(dbcollection.find({"CorrelationId":v}))
    if len(documents) == 0:
        
        print("old",v)
        dbconn1, dbcollection1 = utils.open_dbconn("PS_IngestedData")
        try:
            colsList = list(dbcollection1.find({"CorrelationId":v}))[0]
            dbconn1.close()
            columns = colsList["ColumnsList"]
            uniquedetails = {}
            for col in columns:
                uniquedetails[col] = {"Message":{},"Percent":100.0}
            valrecords = {"EmptyColumns":[],"msg":"","Records":[]}
            Id = str(uuid.uuid4())
            dbcollection.insert_many([{"_id": Id,
                                   "CorrelationId": v,
                                   "UniquenessDetails" : uniquedetails,
                                   "ValidRecordsDetails" : valrecords,
                                   "CreatedByUser": "SYSTEM",
                                   "ModifiedOn": datetime.now().strftime('%Y-%m-%d %H:%M:%S')
                                   }])
            migrated_corrs.append(v)
        except:
          print(v,"failed file upload")
          migrated_corrs.append(v)
          pass
        
    else:
        print(v,".....")
with open("migratedids.txt","w") as f:
    f.write(str(migrated_corrs))
        

    
         











