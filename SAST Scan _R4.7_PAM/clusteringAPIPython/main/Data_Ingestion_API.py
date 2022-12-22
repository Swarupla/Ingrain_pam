# -*- coding: utf-8 -*-
"""
Created on Mon Jul 27 16:18:14 2020

@author: s.siddappa.dinnimani
"""

import configparser, os
import sys
import platform
if platform.system() == 'Linux':
    conf_path = '/main/pythonconfig.ini'
    EntityConfig_path = '/main/pheonixentityconfig.ini'
    work_dir = '/'
    #modelPath = '/saveModels/'
    # pylogs_dir = '/IngrAIn_Python/PyLogs/'
elif platform.system() == 'Windows':
    conf_path = '\main\pythonconfig.ini'
    EntityConfig_path = '\main\pheonixentityconfig.ini'
												
    work_dir = '\\'
    from requests_negotiate_sspi import HttpNegotiateAuth
    #modelPath = '\saveModels\\'
    # pylogs_dir = '\IngrAIn_Python\PyLogs\\'
#import configparser, os

config = configparser.RawConfigParser()
if platform.system() == 'Linux':
    configpath = str(os.getcwd()) + str(conf_path)
elif platform.system() == 'Windows':
    configpath = str(os.getcwd()) + str(conf_path)

from main import file_encryptor

try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)
        #config.read(configpath) 
EntityConfig = configparser.RawConfigParser()
EntityConfig.read(str(os.getcwd()) + str(EntityConfig_path))

import sys
import pandas as pd
import numpy as np
import json
from datetime import datetime
import os
import requests
from urllib.parse import urlencode
from collections import ChainMap
import inspect
#currentdir = os.path.dirname(os.path.abspath(inspect.getfile(inspect.currentframe())))
#parentdir = os.path.dirname(currentdir)
#sys.path.insert(0,parentdir) 
#import utils
from SSAIutils import utils
import base64
from main import EncryptData
import warnings
warnings.filterwarnings("ignore")
min_df = 20
auth_type = utils.get_auth_type()

def getcust(inid):
    from uuid import UUID
    newuuid=UUID(str(inid)).bytes
    return Binary(bytes(bytearray(newuuid)), UUID_SUBTYPE)

def Read_Data(correlationId,pageInfo,userId,uniId,ClientID,DCUID,ServiceID,userdefinedmodelname,invokeIngestData,flag = False): 
#    invokeIngestData = eval(invokeIngestData)
    errFlag = False
    parent = invokeIngestData['Parent']
    mapping_flag = invokeIngestData['mapping_flag']
    auto_retrain = False
    lastDateDict = {}
    if invokeIngestData['Flag'] == "AutoRetrain":
        auto_retrain = True
    MappingColumns = {}
    if mapping_flag == "False":    
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        # Entities  Implementation
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        MultiSourceDfs = {}
        #utils.updQdb(correlationId, 'P', '15', pageInfo, userId,uniId)
        if invokeIngestData['pad'] != "null" and invokeIngestData['pad'] != "":
            global entityDfs 
            entityDfs = {}
            lastDateDict['Entity'] = {}
            x = eval(invokeIngestData['pad'])
            entityAccessToken,status_code = utils.getEntityToken()        
            if status_code != 200:
                entityDfs['Entity'] = "Phoenix API is returned " + str(status_code) + " code, Error Occured while calling Phoenix API"  
            else:
                if x["method"] == "AGILE":
                    for entity, delType in x["Entities"].items():
                        if auto_retrain:
                            start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            end = datetime.today().strftime('%m/%d/%Y')                           
                        else:										
                            start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
				   
                        k = {"ClientUId": invokeIngestData["ClientUID"],
                             "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                            }
                        if entity in ['Defect','Task','Risk','Issue']:
                            delType = "Agile"
                        if delType == "Agile":
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                            
                            agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                            agileAPI = agileAPI.format(urlencode(k))
						
    #                        CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,config,deltype = None)
                            utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            
						   
                        elif delType == "AD/Agile":
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)

                            agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                            agileAPI = agileAPI.format(urlencode(k))
                            utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            
                        elif delType == "ALL":
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                      
                            agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                            agileAPI = agileAPI.format(urlencode(k))
                            if entity != 'Iteration':
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            else:
                                utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)

                        else:
                            raise Exception("please provide correct method")
                               
                elif x["method"] == "DEVOPS":
                    for entity, delType in x["Entities"].items():
                        if delType in ["Devops", "AD/Devops", "ALL"]:

                            if auto_retrain:
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.today().strftime('%m/%d/%Y')
                            else:
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUID"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                 }
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                            devopsAPI = EntityConfig['CDMConfig']['CDMAPI']
                            devopsAPI = devopsAPI.format(urlencode(k))
                            if entity != 'Iteration':
                                utils.CallCdmAPI(devopsAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            else:
                                utils.IterationAPI(devopsAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
											  

                elif x["method"] == "AD":
                    for entity, delType in x["Entities"].items():
                        if delType in ["AD","AD/Agile","AD/Devops", "ALL","AD/PPM"]:

                            if auto_retrain:
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.today().strftime('%m/%d/%Y')
                            else:
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUID"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                 }
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                            AdApi = EntityConfig['CDMConfig']['CDMAPI']
                            AdApi = AdApi.format(urlencode(k))
                            if entity != 'Iteration':
                                utils.CallCdmAPI(AdApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            else:
                                utils.IterationAPI(AdApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                                
                elif x["method"] == "OTHERS":
                    for entity, delType in x["Entities"].items():
                        if delType in ["AD","AD/Agile","AD/Devops", "ALL","Others","Others/PPM"]:
                            if auto_retrain:
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.today().strftime('%m/%d/%Y')
                            else:
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUID"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                 }
                            entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                            OthersApi = EntityConfig['CDMConfig']['CDMAPI']
                            OthersApi = OthersApi.format(urlencode(k))
                            if entity != 'Iteration':
                                utils.CallCdmAPI(OthersApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            else:
                                utils.IterationAPI(OthersApi,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                elif x["method"] == "PPM":
                        for entity, delType in x["Entities"].items():
                            if auto_retrain:																																				  
                                start = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                previousLastDate = start
                           																											
                            else:																						 
                                start = datetime.strptime(str(x["startDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                                end = datetime.strptime(str(x["endDate"]), '%m/%d/%Y').strftime('%m/%d/%Y')
                            k = {"ClientUId": invokeIngestData["ClientUID"],
                                 "DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]
                                }
                        
                            if delType == "Agile/PPM":
                                delType = "Agile"
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                            
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)                           
                            elif delType == "AD/PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)                            
                            elif delType == "PPM":
                                entityArgs = utils.getEntityArgs(invokeIngestData,entity,start,end,x["method"],delType)                         
                                agileAPI = EntityConfig['CDMConfig']['CDMAPI']
                                agileAPI = agileAPI.format(urlencode(k))
                                if entity != 'Iteration':
                                    utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                                else:
                                    utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken,correlationId)
                            else:
                                raise Exception("Method is not PPM. Please verify")
                                
                else:
                    entityDfs = {}
                
            MultiSourceDfs['Entity'] = entityDfs							  
                
        else:
            MultiSourceDfs['Entity'] = {}
        #utils.updQdb(correlationId, 'P', '40', pageInfo, userId,uniId)
        
       
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        # file Upload
        # +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if invokeIngestData['fileupload']['fileList'] != 'null':  ###CHANGES
            fileDfs = {}
            x = invokeIngestData['fileupload']['fileList']
            if platform.system() == 'Linux':
                x = x.replace('\\', '//')
                x = eval(x)
            elif platform.system() == 'Windows':
                x = [x.strip('[').strip(']').strip('"')]
            
            for filepath in x:
                
                if filepath.endswith('.csv') or filepath.endswith('.csv.enc'):
#                    data_t = utils.read_csv(filepath)
                    data_t = utils.read_csv(filepath)
                    if data_t.shape[0] == 0:
                        fileDfs[filepath] = 'No data in the csv. Please upload with data'
                    elif data_t.shape[0] <= min_df:
                        fileDfs[
                            filepath] = "Number of records less than or equal to "+str(min_df)+". Please upload file with more records"
                    else:
                        fileDfs[filepath] = data_t
                elif filepath.endswith('.xlsx') or filepath.endswith('.xlsx.enc'):
                    data_t = utils.read_excel(filepath)
#                    data_t = utils.read_excel(filepath)
                    if data_t.shape[0] == 0:
                        fileDfs[filepath] = 'No data in the csv. Please upload with data'
                    elif data_t.shape[0] <= min_df:
                        fileDfs[
                            filepath] = "Number of records less than or equal to "+str(min_df)+". Please upload file with more records"
                    else:
                        fileDfs[filepath] = data_t
               
            MultiSourceDfs['File'] = fileDfs
        else:
            MultiSourceDfs['File'] = {}
        
        if invokeIngestData['Customdetails']!="null":
           customDfs={'Customdetails':invokeIngestData['Customdetails']}
           AppId=customDfs.get('Customdetails').get('AppId')
           if auth_type == 'AzureAD':
                customurl_token,status_code=utils.CustomAuth(AppId)
           elif auth_type == 'WindowsAuthProvider':
               customurl_token =''
               status_code = 200
               
           try:
               invokeIngestData['Customdetails']['InputParameters'] = eval(invokeIngestData['Customdetails']['InputParameters'])
           except Exception as e:
               error_encounterd = str(e.args[0]) 
           
            
           if status_code==200:
               result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(invokeIngestData.get('Customdetails').get('InputParameters')),headers={'Content-Type': 'application/json',
                                                     'Authorization': (customurl_token)})
               if result.status_code==200:
                   data_json=result.json()
                   data=pd.DataFrame(data_json)
                   if 'DeliveryConstruct' in data.columns:
                       data.drop('DeliveryConstruct',axis=1,inplace=True)
                   if data.shape[0]==0:
                       MultiSourceDfs['Custom'] = {"Custom":"DataFrame is empty. No Data Available for Selection"}
                       message = "No. of records less than or equal to 19. Please validate the data."
                       utils.updQdb(correlationId,'E',str(message),pageInfo,userId,UniId=uniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                       errFlag = True

                   elif data.shape[0]<=min_df:
                       MultiSourceDfs['Custom'] = {"Custom":"No of rows in the data is less than "+str(min_df)}
                       message = "No. of records less than or equal to 19. Please validate the data."
                       utils.updQdb(correlationId,'E',str(message),pageInfo,userId,UniId=uniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                       errFlag = True    
                       
                   else:
                       if 'DeliveryConstruct' in data.columns:
                           assocColumn = 'DeliveryConstruct'
                           col = data[assocColumn]
                           data['DeliveryConstructUId'] = np.nan
                           for i in range(len(data[assocColumn])):
                               if col[i]!='' and col[i] != None:
                                   val = []
                                   for k in range(len(col[i])):
                                       if 'DeliveryConstructUId' in col[i][k].keys():
                                           val.append(col[i][k]['DeliveryConstructUId'])
                                   data['DeliveryConstructUId'][i] = val
                           data = data.explode('DeliveryConstructUId').fillna('')
                           data.drop(columns=assocColumn,inplace=True)
                           MultiSourceDfs['Custom'] = {"Custom":data}
                       else:
                           MultiSourceDfs['Custom'] = {"Custom":data}

#                       if "DateColumn" not in data.columns:
#                           MultiSourceDfs['Custom'] = {"Custom": "DateColumn is not found."}
#                       else:
#                           data['DateColumn'] = pd.to_datetime(data['DateColumn'])
#                           lastDateDict["Custom"] = {"DateColumn":data['DateColumn'].max().strftime('%Y-%m-%d %H:%M:%S')}
#                           MultiSourceDfs['Custom'] = {"Custom":data}
                     
               else:
                    MultiSourceDfs['Custom'] = {"Custom":"The Status Code after hitting post token generation is "+str(result.status_code)}
           else:
                 MultiSourceDfs['Custom'] = {"Custom":"The Token is not Authorized"}
        elif 'CustomSource' in invokeIngestData.keys() and invokeIngestData['CustomSource'] != 'null':
                global customdataDfs
                customdataDfs = {}
                lastDateDict = {}
                customdata = invokeIngestData['CustomSource']
                t = base64.b64decode(customdata)
                dtext = json.loads(EncryptData.DescryptIt(t))
                if dtext["Type"] == "API":
                    entityArgs = dtext["BodyParam"]
                    TargetNode = dtext["TargetNode"].split('.')
                    hadoopApi = dtext["ApiUrl"]
                    if dtext["KeyValues"] != {}:
                        k={"ClientUId": dtext["KeyValues"]["ClientUId"],"DeliveryConstructUId": dtext["KeyValues"]["DeliveryConstructUId"]}
                    else:
                        k = {"ClientUId": invokeIngestData["ClientUID"],"DeliveryConstructUId": invokeIngestData["DeliveryConstructUId"]}
                    hadoopApi = hadoopApi.format(urlencode(k))
                    Authtoken = dtext["Authentication"]
                    if Authtoken["UseIngrainAzureCredentials"] == True:
                        entityAccessToken = Authtoken["Token"]
                    else:
                        if Authtoken["Type"] == 'AzureAD':
                            TokenURL = Authtoken['AzureUrl']
                            headers = {"Content-Type": "application/x-www-form-urlencoded"}
                            payload = Authtoken['AzureCredentials']
                            try:
                                r = requests.post(TokenURL, data=payload, headers=headers)
                                tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
                                token = tokenjson["access_token"]
                            except:
                                token = None
                            entityAccessToken = token
                        elif Authtoken["Type"] == 'Token':
                            entityAccessToken = Authtoken["Token"]

                    customdataDfs = utils.Customsourcedata(hadoopApi,TargetNode,entityArgs,entityAccessToken,customdataDfs,lastDateDict,Incremental=False)
                    if isinstance(customdataDfs, pd.DataFrame):
                        MultiSourceDfs['Custom'] = {"Custom":customdataDfs}
                    else:
                        MultiSourceDfs['Custom'] = {"Custom":customdataDfs['custom']}
                elif dtext["Type"] == "CustomDbQuery":
                    try:
                        query = dtext['Query']
                        connection = utils.open_phoenixdbconn()
                        data = connection.command(eval(query))
                        queryDF = pd.DataFrame(data['cursor']['firstBatch'])
                        columns_list=[]
                        for col in queryDF.columns: 
                            m=(queryDF[col].map(lambda x : type(x).__name__)=='UUID').any()
                            if m:
                                columns_list.append(col)
                                
                            if len(columns_list)>0:
                                for x in columns_list:        
                                    queryDF[x]=queryDF[x].apply(str)
                        for col in queryDF.columns: 
                            try:
                                if queryDF[col].dtype == 'object' and 'DateTime' in queryDF[col].iloc[0].keys():
                                    queryDF[col] = [pd.to_datetime(d['DateTime']) for d in queryDF[col]]
                            except Exception as e:
                                error_encounterd = str(e.args[0])
                            try:
                                queryDF[[dtext["DateColumn"]]] = queryDF[dtext["DateColumn"]].apply(lambda x: x['DateTime'])
                                lastDateDict["CustomSource"] = {dtext["DateColumn"]:queryDF[dtext["DateColumn"]].max().strftime('%Y-%m-%d %H:%M:%S')}
                            except Exception as e:
                                error_encounterd = str(e.args[0])
                        MultiSourceDfs['Custom'] = {"Custom":queryDF}
                    except:
                        from uuid import UUID
                        from pymongo.errors import InvalidBSON
                        query = dtext['Query']	
                        import re
                        if 'ISODate' in query:
                            m = re.search('ISODate(.*})',query)
                            m = m.group(0).split('}')[0]
                            date_withoutkey = m.split('ISODate')[1]
                            new_date = datetime.fromisoformat(eval(date_withoutkey))
                            new_date = repr(datetime.strptime(str(new_date), '%Y-%m-%d %H:%M:%S').strftime('%Y-%m-%d %H:%M:%S').replace(' 0',' ').replace('-0','-'))
                            query = query.replace(m,new_date,1)
                            query = eval(query)
                            for key,value in query["pipeline"][0]["$match"].items():
                                for key1,value1 in value.items():
                                    if eval(new_date)==value1:
                                        query["pipeline"][0]["$match"][key][key1] = datetime.strptime(str(query["pipeline"][0]["$match"][key][key1]), '%Y-%m-%d %H:%M:%S')

                        else:						
                            query = eval(query)
                        #print(query["pipeline"][0]["$match"]["ModifiedOn.DateTime"]["$lt"])
                        regexUuid = "[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[89aAbB][a-f0-9]{3}-[a-f0-9]{12}"
                        from bson.binary import Binary, UUID_SUBTYPE
                        for key,value in query["pipeline"][0]["$match"].items():
                            #print(bool(re.match(regexUuid, str(value))))
                            if bool(re.match(regexUuid, str(value))):
                                query["pipeline"][0]["$match"][key] = getcust(value)
                        connection = utils.open_phoenixdbconn()
                        data = connection.command(query)		
                        queryDF = pd.DataFrame(data['cursor']['firstBatch'])
                        for col in queryDF.columns:
                            try:
                                if queryDF[col].dtype == 'object' and 'DateTime' in queryDF[col].iloc[0].keys():
                                    queryDF[col] = [pd.to_datetime(d['DateTime']) for d in queryDF[col]]
                            except Exception as e:
                                error_encounterd = str(e.args[0])
                            try:
                                queryDF[[dtext["DateColumn"]]] = queryDF[dtext["DateColumn"]].apply(lambda x: x['DateTime'])
                                lastDateDict["CustomSource"] = {dtext["DateColumn"]:queryDF[dtext["DateColumn"]].max().strftime('%Y-%m-%d %H:%M:%S')}
                            except Exception as e:
                                error_encounterd = str(e.args[0])
                        lastDateDict["CustomSource"] = {dtext["DateColumn"]:queryDF[dtext["DateColumn"]].max().strftime('%Y-%m-%d %H:%M:%S')}
                        MultiSourceDfs['Custom'] = {"Custom":queryDF}
        else:
             MultiSourceDfs['Custom']={}
        #logger = utils.logger('Get', correlationId)
        #entity data identification
        EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])

        j = []
        if MultiSourceDfs['File'] != {}:
            for k,v in MultiSourceDfs['File'].items():
                if  type(v) != str:
                    b = list(EntityMappingColumns.intersection(v.columns))
                    if len(b) == 2:
                        MultiSourceDfs['Entity'][k] = v 
                        j.append(k)
        for i in j:
            del MultiSourceDfs['File'][i] 
            
        if len(MultiSourceDfs['File']) > 1 and MultiSourceDfs['Entity'] == {}:
            for k,v in MultiSourceDfs['File'].items():
                file_name = os.path.basename(k).split('.')[0][36:]
                Rename_cols = dict(ChainMap(*list(map(lambda x: {x: x + file_name}, list(v.columns)))))
                v.rename(columns=Rename_cols, inplace=True)
                MultiSourceDfs['File'][k] = v.copy()
                
        if MultiSourceDfs['File'] != {} and MultiSourceDfs['Custom'] != {}:
            entity_custom = str(invokeIngestData.get('Customdetails').get('InputParameters')['ServiceType'])
            for k,v in MultiSourceDfs['Custom'].items():
                if  type(v) != str:
                     MultiSourceDfs['File'][entity_custom+"."+k] = v 
            MultiSourceDfs['Custom'] = {}          
        
        if MultiSourceDfs['File'] != {} and MultiSourceDfs['Entity'] != {}:
             z = [os.path.basename(file).split('.')[0] for file in  list(MultiSourceDfs['File'].keys())]
             n = ",".join(z) 
             #utils.updQdb(correlationId, 'E',"Uploaded "+n+" files contains non CDM entity data, Mapping is not psossible.Kindly Upload entity related data", pageInfo, userId,uniId)
        if  MultiSourceDfs['File'] == {} and MultiSourceDfs['Entity'] != {}:
            MultiSourceDfs, MappingColumns  = utils.TransformEntities(MultiSourceDfs,invokeIngestData["ClientUID"],invokeIngestData["DeliveryConstructUId"],EntityMappingColumns,parent,auto_retrain)
    return MultiSourceDfs,MappingColumns,lastDateDict,errFlag
