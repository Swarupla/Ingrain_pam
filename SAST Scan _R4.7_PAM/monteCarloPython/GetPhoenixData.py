# -*- coding: utf-8 -*-
"""
Created on Thu Oct 22 18:27:22 2020

@author: s.siddappa.dinnimani
"""
import platform
if platform.system() == 'Windows':
    from requests_negotiate_sspi import HttpNegotiateAuth

from dateutil import relativedelta
import pandas as pd
import requests
import numpy as np
from collections import ChainMap
import utils 
import file_encryptor
import configparser, os
import requests
import json
import datetime
from urllib.parse import urlencode
from utils import open_dbconn


config = configparser.RawConfigParser()
conf_path = "/pythonconfig.ini"
configpath = str(os.getcwd()) + str(conf_path)

try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)

entityconf_path = "/pheonixentityconfig.ini"

EntityConfig = configparser.RawConfigParser()
EntityConfigpath = str(os.getcwd()) + str(entityconf_path)
try:
    EntityConfig.read(EntityConfigpath)
except UnicodeDecodeError:
    EntityConfig = file_encryptor.get_configparser_obj(EntityConfigpath)
    
def min_data():
    min1=config['Min_Data']['min_data']
    
    return int(min1)

def get_auth_type():
    auth=config['GenericSettings']['authProvider']
    return auth


def getMetricAzureToken():
    TokenURL = config['METRIC']['AdTokenUrl']
    headers = {
        "Content-Type": "application/x-www-form-urlencoded"
    }
    payload = {
        "scope": config['METRIC']['scope'],
        "grant_type": config['METRIC']['grant_type'],
        "resource": config['METRIC']['resource'],
        "client_id": config['METRIC']['client_id'],
        "client_secret": config['METRIC']['client_secret'],
    }
    try:
        r = requests.post(TokenURL, data=payload, headers=headers)
        tokenjson = eval(r.content.decode('utf8').replace("'", '"'))
        token = tokenjson["access_token"]
    except:
        token = None
    return token, r.status_code

def maxdatapull(tempId):
    dbconn, dbcollection = open_dbconn('TemplateData')
    if tempId:
        args = dbcollection.find_one({"TemplateData": tempId})
        if "MaxDataPull" in args:
            data_points = args["MaxDataPull"]
        else:
            data_points = int(config['maxpull']['records'])
    else:
        data_points = int(config['maxpull']['records'])
    return int(data_points)

def CallCdmAPI(agileAPI,tempId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken):
    auth_type = get_auth_type()
    if auth_type == 'AzureAD':

        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
    elif auth_type == 'WindowsAuthProvider':
        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                  auth=HttpNegotiateAuth())
   if resp.status_code == 200:
        if resp.text == "No data available":
            if auto_retrain:
                entityDfs[entity] = "No incremental data available"
            else:
                entityDfs[entity] = "Data is not available for your selection"
        else:
            if resp.json()['TotalRecordCount'] != 0:
                maxdata = int(config['maxpull']['records'])
                x = 1
                nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
                if maxdata < resp.json()['TotalRecordCount'] and nonprod:
                    entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                    if auth_type == 'AzureAD':
                        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                    elif auth_type == 'WindowsAuthProvider':
                        resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())                    
                    while maxdata > resp.json()['TotalRecordCount']:
                        x = x + 1
                        entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                        if auth_type == 'AzureAD':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                        elif auth_type == 'WindowsAuthProvider':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
                #workItem = resp.json()['Entity']
                entityDataframe = pd.DataFrame({})
                numberOfPages = resp.json()['TotalPageCount']
                TotalRecordCount = resp.json()['TotalRecordCount']
                i = 1
                while i < numberOfPages:
                    entityArgs.update({"PageNumber": i + 1})
                    entityArgs.update({"TotalRecordCount": TotalRecordCount})
                    if auth_type == 'AzureAD':
                        entityresults = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                    elif auth_type == 'WindowsAuthProvider':
                        entityresults = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                                      auth=HttpNegotiateAuth())
                            
                    if entityresults.status_code == 200:
                        entityData = entityresults.json()['Entity']
                        if entityData != []:
                            df = pd.DataFrame(entityData)
                        entityDataframe = pd.concat([entityDataframe,df]).reset_index(drop=True)
                    i = i + 1 
                if entityDataframe.empty:
                    if auto_retrain:
                        entityDfs[entity] = "No incremental data available"
                    else:
                        entityDfs[entity] = "Data is not available for your selection"
                elif entityDataframe.shape[0] <= min_data() and not auto_retrain:                    				
                    entityDfs[entity] = "Number of records less than or equal to "+str(min_data())+". Please upload file with more records"
                else:
                    entityDataframe["modifiedon"] = pd.to_datetime(entityDataframe["modifiedon"], format="%Y-%m-%d %H:%M", exact=True)
                    lastDateDict['Entity'][entity] = pd.to_datetime(entityDataframe["modifiedon"].max()).strftime('%Y-%m-%d %H:%M:%S')
                    entityDataframe.rename(columns={"modifiedon": 'DateColumn'}, inplace=True)
                    entityDfs[entity] = entityDataframe
                    #######itemexternalid change############
                    if 'workitemassociations' in entityDfs[entity].columns:
                        assocColumn = 'workitemassociations'
                    elif (entity != "CodeBranch" and entity != "Task"):
                        assocColumn = entity.lower()+"associations"
                    else:
                        if entity == "Task":
                            assocColumn = "deliverytaskassociations"
                    #if assocColumn != "codebranchassociations":
                    if entity != "CodeBranch": 
                        col = entityDfs[entity][assocColumn]
                        entityDfs[entity]['itemexternalid'] = np.nan
                        for i in range(len(entityDfs[entity][assocColumn])):
                            if col[i]!='' and col[i] != None:
                                val = []
                                try:
                                    for k in range(len(eval(col[i]))):
                                        if 'ItemExternalId' in eval(eval(col[i])[k]).keys():
                                            val.append(eval(eval(col[i])[k])['ItemExternalId']) 
                                except:
                                    for k in range(len((col[i]))):
                                        if 'ItemExternalId' in ((col[i])[k]).keys():
                                            val.append(((col[i])[k])['ItemExternalId'])                                 
                                entityDfs[entity]['itemexternalid'][i] = val
                        entityDfs[entity] = entityDfs[entity].explode('itemexternalid').fillna('')
                        entityDfs[entity].drop(columns=assocColumn,inplace=True)                        
                    ##########itemexternalid change completed##########
            else:
                entityDfs[entity] = "Data is not available for your selection"
    else:
        e = "Phoenix API is returned " + str(resp.status_code) + " code, Error Occured while calling API or API returning Null"
        entityDfs[entity] = e
    return



def IterationAPI(agileAPI,tempId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken):
    final_df = pd.DataFrame()
    api_error_flag = True
    IterationTypes = eval(EntityConfig['CDMConfig']['IterationTypes'])
    auth_type = get_auth_type()
    for j in range(len(IterationTypes)):
        entityArgs.update({"IterationTypeUId": IterationTypes[j]})
        entityArgs.update({"PageNumber": 1})
        entityArgs.update({"TotalRecordCount": 0})
        if auth_type == 'AzureAD':
            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',
                                  'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
        elif auth_type == 'WindowsAuthProvider':
            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},
                                 auth=HttpNegotiateAuth())
        if resp.status_code == 200:
            api_error_flag = False
            if resp.text != "No data available":
                if resp.json()['TotalRecordCount'] != 0:
                    maxdata = int(config['maxpull']['records'])
                    x = 1
                    nonprod = 'dev' in config['BaseUrl']['myWizardAPIUrl'] or 'stage' in config['BaseUrl']['myWizardAPIUrl'] or 'uat' in config['BaseUrl']['myWizardAPIUrl']
                    if maxdata < resp.json()['TotalRecordCount'] and nonprod:
                        entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                        if auth_type == 'AzureAD':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',    'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                        elif auth_type == 'WindowsAuthProvider':
                            resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
                        while maxdata > resp.json()['TotalRecordCount']:
                            x = x + 1
                            entityArgs['FromDate'] = (datetime.strptime(str(entityArgs['ToDate']), '%m/%d/%Y')-relativedelta.relativedelta(months=1*x)).strftime('%m/%d/%Y')
                            if auth_type == 'AzureAD':
                                resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json',   'authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                            elif auth_type == 'WindowsAuthProvider':
                                resp = requests.post(agileAPI, data=json.dumps(entityArgs),headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']}, auth=HttpNegotiateAuth())
                    #workItem = resp.json()['Entity']
                    entityDataframe = pd.DataFrame({})
                    numberOfPages = resp.json()['TotalPageCount']
                    TotalRecordCount = resp.json()['TotalRecordCount']
                    i = 1    
                    while i < numberOfPages:
                        entityArgs.update({"PageNumber": i + 1})
                        entityArgs.update({"TotalRecordCount": TotalRecordCount})
                        if auth_type == 'AzureAD':
                            entityresults = requests.post(agileAPI, data=json.dumps(entityArgs), headers={'Content-Type'  : 'application/json','authorization' : 'bearer {}'.format(entityAccessToken),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                         
                        elif auth_type == 'WindowsAuthProvider':
                            entityresults = requests.post(agileAPI, data=json.dumps(entityArgs), headers={'Content-Type'  : 'application/json','AppServiceUId':config['HadoopApi']['AppServiceUId']},auth=HttpNegotiateAuth())
                                         
                        if entityresults.status_code == 200:
                            entityData = entityresults.json()['Entity']
                            if entityData != []:
                                df = pd.DataFrame(entityData)
                            entityDataframe = pd.concat([entityDataframe,df]).reset_index(drop=True)
                        i = i + 1
                else:
                    entityDataframe  = pd.DataFrame()
            else:
                entityDataframe  = pd.DataFrame()
            final_df = pd.concat([final_df,entityDataframe]).reset_index(drop=True)           
    if final_df.empty:
        if not api_error_flag:
            if auto_retrain:
                entityDfs[entity] = "No incremental data available"
            else:
                entityDfs[entity] = "Data is not available for your selection"
        else:
           entityDfs[entity] =  "Phoenix API is returned " + str(resp.status_code) + " code, Error Occured while calling API or API returning Null"
    elif final_df.shape[0] <= min_data() and not auto_retrain:                    				
        entityDfs[entity] = "Number of records less than or equal to "+str(min_data())+". Please upload file with more records"
    else:
        final_df["modifiedon"] = pd.to_datetime(final_df["modifiedon"], format="%Y-%m-%d %H:%M", exact=True)
        lastDateDict['Entity'][entity] = pd.to_datetime(final_df["modifiedon"].max()).strftime('%Y-%m-%d %H:%M:%S')
        final_df.rename(columns={"modifiedon": 'DateColumn'}, inplace=True)
        entityDfs[entity] = final_df
        assocColumn = entity.lower()+"associations"
        col = entityDfs[entity][assocColumn]
        entityDfs[entity]['itemexternalid'] = np.nan
        for i in range(len(entityDfs[entity][assocColumn])):
            if col[i]!='' and col[i] != None:
                val = []
                try:
                    for k in range(len(eval(col[i]))):
                        if 'ItemExternalId' in eval(eval(col[i])[k]).keys():
                            val.append(eval(eval(col[i])[k])['ItemExternalId']) 
                except:
                    for k in range(len((col[i]))):
                        if 'ItemExternalId' in ((col[i])[k]).keys():
                            val.append(((col[i])[k])['ItemExternalId'])                                 
                entityDfs[entity]['itemexternalid'][i] = val
        entityDfs[entity] = entityDfs[entity].explode('itemexternalid').fillna('')
        entityDfs[entity].drop(columns=assocColumn,inplace=True)
                                               
    return


def DoAd_AgileTransfrom(df,EntityConfig,m,ids,RequiredColumns,flag=None):
    if RequiredColumns == 'WorkItem':
        cols_Req = eval(EntityConfig[RequiredColumns]["RequiredColumns"]).split(',') + m
    elif RequiredColumns == 'deliverytask':
        cols_Req = eval(EntityConfig["task"]["RequiredColumns"]).split(',') + m
    else:
        cols_Req = eval(EntityConfig[RequiredColumns.casefold()]["RequiredColumns"]).split(',') + m
    if ids != 'codebranchuid' and ids != 'environmentuid' and ids != 'assignmentuid' and ids != 'workitemuid':
        itemexternalid =  m[1]

    dfs = []
    if RequiredColumns == 'WorkItem':
        '''
        val = "value"
        dispName = "displayname"
        glist = set(df[dispName].dropna().unique())
        #if flag == 'agileusecase':
        df4=pd.DataFrame()
        df5=pd.DataFrame()
        df7=pd.DataFrame()
        df2=df[['value','externalvalue','displayname']]
        df2=df2[df2['displayname'].isin(['Iteration'])]
        df2.reset_index(drop=True, inplace=True)
        df2=df2.drop(['displayname'], axis = 1)
        df2.rename(columns={'value':'Iteration','externalvalue':'Iterationname'}, inplace=True)
        df2=df2.replace('',np.nan)
        df2 = df2[df2['Iteration'].notna()]
        df4=pd.Series(df2.Iterationname.values,index=df2.Iteration).to_dict()
        df2.reset_index(drop=True, inplace=True)
        df3=df[['value','externalvalue','displayname']]
        df3=df3[df3['displayname'].isin(['Release'])]
        df3.rename(columns={'value':'Release','externalvalue':'Releasename'}, inplace=True)
        df3=df3.drop(['displayname'], axis = 1)
        df3=df3.replace('',np.nan)
        df3 = df3[df3['Release'].notna()]
        df3.reset_index(drop=True, inplace=True)
        df5=pd.Series(df3.Releasename.values,index=df3.Release).to_dict()
        df6=df[['value','idvalue','displayname']]
        df6=df6[df6['displayname'].isin(['State'])]
        df6.reset_index(drop=True, inplace=True)
        df6=df6.drop(['displayname'], axis = 1)
        df6.rename(columns={'value':'StateUID','idvalue':'State'}, inplace=True)
        df6=df6.replace('',np.nan)
        df7=pd.Series(df6.StateUID.values,index=df6.State).to_dict()
        '''
    else:
        if ids != 'builduid' and ids !="assignmentuid" and ids != "resourceuid":
            val = "fieldvalue"
            dispName = "fieldname"
            glist = set(df[dispName].dropna().unique())

    if "modifiedon" in df.columns:
        df.rename(columns={"modifiedon": 'DateColumn'}, inplace=True)

    cols_Req.remove("modifiedon")
    if 'DateColumn' not in cols_Req:
        cols_Req = cols_Req + ['DateColumn']        
    if "modifiedon" in  cols_Req:
        cols_Req.remove("modifiedon") 
    if ids != 'builduid' and ids !="assignmentuid" and ids != "resourceuid" and ids != 'workitemuid':
        
        glist = glist.union(set(cols_Req))
    temp_list = list()
    if RequiredColumns != 'WorkItem':
        for x in df[ids].unique():
            df1 = df[df[ids].isin([x])].sort_values(by=['DateColumn'],ascending=False)
            df1 = df1[df1.DateColumn.isin([list(df1.DateColumn)[0]])]
            df1 = df1[cols_Req]
            df1 = df1.loc[:,~df1.columns.duplicated()]
            temp_list.append(df1)
            if ids == 'codebranchuid' or ids == 'builduid' or ids == 'environmentuid'  or ids == 'assignmentuid' or ids == 'resourceuid'or ids == 'assignmentuid':
                dfs.append(df1)
            else:
                df1['key'] = df1[itemexternalid].astype(str)+ df1[dispName]
                df1.drop_duplicates(inplace = True)
                temp_ = []
                for s in df1.itemexternalid.unique():
                    temp = df1[df1.itemexternalid.isin([s])]
                    temp = temp.T
                    cols = temp.loc[dispName].dropna().to_dict()
                    temp.rename(columns = cols,inplace = True)
                    temp.drop(index = ['key'], inplace = True)
                    inx = set(temp.index).difference({val,dispName})
                    for col in inx:
                        temp[col] =  pd.Series(temp.loc[col].unique()[0],index= temp.index)
                    inx =  list(inx)
                    t = list(cols.values()) + inx
                    inx.append(dispName)
                    temp.drop(index =inx,inplace = True)
                    temp = temp[t]
                    temp = temp.loc[:,~temp.columns.duplicated()]
                    glist = glist.difference({val,dispName})
                    j = pd.DataFrame(data = None, index = temp.index, columns = glist)
                    j.loc[val] = temp.loc[val]
                    temp_.append(j)
                df_uid= pd.concat(temp_)
                dfs.append(df_uid)  
         
        entity_df = pd.concat(dfs)
    else:
        entity_df = df
    '''
    if RequiredColumns == 'WorkItem':
        try:
            d = {oldk: oldv for oldk, oldv in df4.items()}
            entity_df['Iterationname'] = entity_df['Iteration'].map(d)
        except:
            pass
        try:
            e= {oldk: oldv for oldk, oldv in df5.items()}
            entity_df['Releasename'] = entity_df['Release'].map(e)
        except:
            pass
        try:
            f= {oldv: oldk for oldk, oldv in df7.items()}
            entity_df['StateUID'] = entity_df['State'].map(f)
        except:
            pass
    '''
    entity_df.reset_index(drop = True,inplace = True)
    return entity_df


def TransformEntities(MultiSourceDfs,EntityMappingColumns,parent,Incremental,auto_retrain,flag = False,correlationId = None):							
    min_df = min_data() 
    EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])
    otherReqCols = eval(EntityConfig['CDMConfig']['otherReqCols'])
    uids = [p.replace("externalid","uid") for p in EntityMappingColumns ]
    #productinstance = set([p.replace("externalid","productinstances_productinstanceuid") for p in EntityMappingColumns ])
    MappingColumns = {}
    for k,v in MultiSourceDfs['Entity'].items():
        otherReqCols = eval(EntityConfig['CDMConfig']['otherReqCols'])			   
        if  type(v) != str:
            ids = list(set(uids).intersection(v.columns))
            if 'workitemuid' in ids:
                 ids = 'workitemuid'
            else:
                if len(ids) > 1:
                    ids = list(set([k.lower()+"uid"]).intersection(ids))[0]
                else:
                    ids = ids[0]
            file_name = ids[:-3]
            b = list(EntityMappingColumns.intersection(v.columns))
            a = file_name + otherReqCols[0]
            otherReqCols.remove(otherReqCols[0])
            otherReqCols.insert(0,a)
            m = list(set(otherReqCols).intersection(b))
            prdctid = "productinstanceuid"						 
            if "workitemexternalid" in m:
                file_name = k.lower()
                if 'createdbyproductinstanceuid' in v.columns:
                    v.rename(columns={'createdbyproductinstanceuid':'productinstanceuid'}, inplace=True)
                if 'stateuid' in v.columns:
                    v.rename(columns = {'stateuid':'State'}, inplace=True)
                
                for col in m:
                    v["prdctid"+"_"+col] = v[prdctid].replace(np.nan,'').astype(str) + v[col].replace(np.nan,'').astype(str)
                v["UniqueRowID"] = v.workitemexternalid + v.itemexternalid
                try:
                    df = v.copy()
                    #v = transformCNV(v,cid,dcuid,k,WorkItem = True)
                except:
                    v = df.copy()
            else:
                if k != 'Environment' and k != "Observation":
                    if 'createdbyproductinstanceuid' in v.columns:
                        v.rename(columns={'createdbyproductinstanceuid':'productinstanceuid'}, inplace=True)
                    v["UniqueRowID"] = v[m[0]] + v[m[1]]
                    for col in m:                  
                        v["prdctid"+"_"+col] = v[prdctid].replace(np.nan,'').astype(str) + v[col].replace(np.nan,'').astype(str)
                v.columns = v.columns.str.replace('[.]', '')
                try:
                    df = v.copy()
                    #v = transformCNV(v,cid,dcuid,k,WorkItem = False)
                except:
                    v = df.copy()

            if v.empty:
                if auto_retrain:
                    v="No Incremental Data Available"
                else:
                    v = "Data is not availble"
                MultiSourceDfs['Entity'][k] = v 
            elif v.shape[0] <= min_df and not auto_retrain:
                v = "Number of records less than or equal to "+str(min_df)+". Please upload file with more records"
                MultiSourceDfs['Entity'][k] = v
            else:
                if flag == None:
                    if type(v) != str:
                        Rename_cols = dict(ChainMap(*list(map(lambda x: {x: x + "_" + file_name}, list(v.columns.astype(str))))))
                        if parent['Name'] == 'null' or  parent['Name'][:-7] == k:
                            if 'DateColumn' in Rename_cols.keys():
                                Rename_cols['DateColumn'] = 'DateColumn'
                        v.rename(columns=Rename_cols, inplace=True)
                        empty_cols = [col for col in v.columns if v[col].dropna().empty]
                        v.drop(columns = empty_cols ,inplace = True)
                        v = v.replace('',np.nan)
                        v = v.replace("",np.nan)
                        if k.endswith(".csv") or k.endswith(".xlsx"):
                            k = os.path.basename(k).split('.')[0][36:]
                            MultiSourceDfs['Entity'][k] = v.copy()
                            MappingColumns[k] = list((map(lambda x: "prdctid"+"_"+ x + "_" + file_name, m))) 
                        else:
                            MultiSourceDfs['Entity'][k] = v.copy()
                            MappingColumns[k] = list((map(lambda x: "prdctid"+"_"+ x + "_" + file_name, m)))
                else:
                      empty_cols = [col for col in v.columns if v[col].dropna().empty]
                      v.drop(columns = empty_cols ,inplace = True)
                      v = v.replace('',np.nan)
                      v = v.replace("",np.nan)
                      for col in v.columns:
                            try:
                                v[col].fillna(v[col].mode()[0], inplace=True)
                            except:
                                v.drop(columns = [col], inplace = True)
                      MultiSourceDfs['Entity'][k] = v.copy()
                      MappingColumns[k] = []

    return MultiSourceDfs, MappingColumns

def getEntityArgs(cid,dcuid,entity,fromdate,todate):
    entityArgs = {
               "ClientUID"            : cid,
               "DeliveryConstructUId" : dcuid,
               "EntityUId"            : eval(EntityConfig[entity.casefold()]['EntityUID']),
               "ColumnList"           : eval(EntityConfig[entity.casefold()]['RequiredColumns']),
               "RowStatusUId"         : eval(EntityConfig['CDMConfig']['rowstatusuid'])['Active'],
               "FieldName"            : eval(EntityConfig[entity.casefold()]['fieldname']),
               "PageNumber"           : "1",
               "TotalRecordCount"     : "0",
               "BatchSize"            : "5000",
               "FromDate"             : fromdate,
               "ToDate"               : todate}
    return entityArgs

def getData(cid,dcuid,tempId):
    k = {
            "ClientUId": cid,
            "DeliveryConstructUId": dcuid
        }
    agileAPI = EntityConfig['CDMConfig']['CDMAPI']
    agileAPI = agileAPI.format(urlencode(k))
  
    t = {
            "Defect" : agileAPI ,
            "Iteration": agileAPI,
            "Deliverable":agileAPI
        }

    MultiSourceDfs = {}
    lastDateDict = {}
    entityDfs = {}
    lastDateDict['Entity'] = {}
    auto_retrain = False
    parent = None
    Incremental = False
    correlationId = None
    parent = {}
    parent['Name'] = 'null'
    entityAccessToken,status_code = getMetricAzureToken()
    todate = datetime.date.today()
    fromdate = todate - datetime.timedelta(days=2*365)
    todate = todate.strftime('%m/%d/%Y')
    fromdate = fromdate.strftime('%m/%d/%Y')
    for a,b in t.items():
        entity = a
        agileAPI = b
        entityArgs = getEntityArgs(cid,dcuid,a,fromdate,todate)
        if entity != "Iteration":
            CallCdmAPI(agileAPI,tempId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
        else:
            IterationAPI(agileAPI,tempId,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
    
    MultiSourceDfs['Entity'] = entityDfs
    EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])
    MultiSourceDfs, MappingColumns  = TransformEntities(MultiSourceDfs,EntityMappingColumns,parent,Incremental,auto_retrain,flag=True,correlationId = correlationId)
    return MultiSourceDfs





