# -*- coding: utf-8 -*-
"""
Created on Mon Oct 12 12:53:01 2020

@author: s.siddappa.dinnimani
"""
import platform

if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    conf_path_pheonix = '/IngrAIn_Python/main/pheonixentityconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    conf_path_pheonix = '\IngrAIn_Python\main\pheonixentityconfig.ini'
    work_dir = '\IngrAIn_Python'

import configparser, os
mainPath = os.getcwd() + work_dir
import sys

sys.path.insert(0, mainPath)
import file_encryptor

config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)
EntityConfig = configparser.RawConfigParser()
EntityConfigpath = str(os.getcwd()) + str(conf_path_pheonix)
EntityConfig.read(EntityConfigpath)

from datetime import datetime

import pandas as pd
import numpy as np
from functools import reduce
from SSAIutils import utils 
from urllib.parse import urlencode
import requests
import json
import datetime

def min_data():
    min1=config['Min_Data']['min_data']
    
    return int(min1)

def Agileaggdata_function(Agile):
    try:
        Agile['EffortEstimated'] = Agile['EffortEstimated'].replace('$', 0)
        Agile['EffortEstimated'] = Agile['EffortEstimated'].replace('$$', 0)
        Agile['EffortEstimated'] = Agile['EffortEstimated'].apply(pd.to_numeric, errors="ignore")
        Agile['BusinessValue'] = Agile['BusinessValue'].replace('$',0)
        Agile['BusinessValue'] = Agile['BusinessValue'].replace('$$',0)
        Agile['BusinessValue'] = Agile['BusinessValue'].apply(pd.to_numeric, errors="ignore")
        Agile['EffortEstimated'] = Agile['EffortEstimated']/3600
        Agile['EffortRemaining'] = Agile['EffortRemaining']/3600
        Agile['EffortCompleted'] = Agile['EffortCompleted']/3600
    except Exception as e:
        error_encounterd = str(e.args[0])
    ##############Story##########################
    print('#################story#####################################')
    
    storydf=pd.DataFrame()
    storydf['Iteration'] = np.nan
    storydf['Iteration'] = storydf['Iteration'].astype('str')
    try:
        Story=(Agile.loc[Agile['WorkItemType'] == 'Story'])
        totalstory=Story.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
        df1=Story.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
        completed=(df1.loc[df1['State'] == 'Closed'])
        completed.rename(columns={'Totalstate':'Completed'}, inplace=True)
        completed=completed.drop(['State','WorkItemType'], axis = 1)
        df2=(pd.merge(totalstory, completed, left_on='Iteration', right_on='Iteration', how='outer'))
        df3=df2.fillna(0)
        df3['Remaining'] = df3["Total"] - df3["Completed"] 
        df3=df3.drop(['WorkItemType'], axis = 1)
        df3.rename(columns={'Completed':'story Completed','Total':'Total Stories','Remaining':'Story Remaining'}, inplace=True)
        df4=Story.groupby(['Iteration','WorkItemType'], as_index=False)['EffortEstimated','EffortRemaining','EffortCompleted'].sum()
        df4=df4.drop(['WorkItemType'], axis = 1)
        df4.rename(columns={'EffortEstimated':'Story EffortEstimated','EffortRemaining':'Story EffortRemaining','EffortCompleted':'Story CompletedEfforts'}, inplace=True)
        df5=(pd.merge(df3, df4, left_on='Iteration', right_on='Iteration', how='outer'))
       # estimated=Story.groupby(['Iteration'], as_index=False)['StoryPointEstimated','StoryPointCompleted'].sum()
        storypoint=Story[['Iteration','StoryPointEstimated','State']]
        storypoint=(storypoint.loc[storypoint['State'] == 'Closed'])
        storypoint=storypoint.groupby(['Iteration'], as_index=False)['StoryPointEstimated'].sum()
        storypoint.rename(columns={'StoryPointEstimated':'Sprint Velocity'}, inplace=True)
        storyestimated=Story.groupby(['Iteration'], as_index=False)['StoryPointEstimated'].sum()
        storyestimated.rename(columns={'StoryPointEstimated':'Planned Story Points'}, inplace=True)
        agg=(pd.merge(storypoint, storyestimated, left_on='Iteration', right_on='Iteration', how='outer'))
        storydf=(pd.merge(df5, agg, left_on='Iteration', right_on='Iteration', how='outer'))
    except:
        storydf=pd.DataFrame()
        storydf['Iteration'] = np.nan
        storydf['Iteration'] = storydf['Iteration'].astype('str')

       
    
    ######################Feature#################
    
    featuredf=pd.DataFrame()
    featuredf['Iteration'] = np.nan
    featuredf['Iteration'] = featuredf['Iteration'].astype('str')
    print("#################feature########")
    try:
        Feature=(Agile.loc[Agile['WorkItemType'] == 'Feature'])
        totalfeature=Feature.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
        df7=Feature.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
            
        completefeatures=(df7.loc[df7['State'] == 'Closed'])
        completefeatures.rename(columns={'Totalstate':'Completed'}, inplace=True)
        completefeatures=completefeatures.drop(['State','WorkItemType'], axis = 1)
        featuredf=(pd.merge(totalfeature, completefeatures, left_on='Iteration', right_on='Iteration', how='outer'))
        featuredf=featuredf.drop(['WorkItemType'], axis = 1)
        featuredf=featuredf.fillna(0)
        featuredf['Remaining'] = featuredf["Total"] - featuredf["Completed"] 
        featuredf.rename(columns={'Completed':'Feature Completed','Total':'Feature Total','Remaining':'Feature Remaining'}, inplace=True)
        #finaldf=(pd.merge(featuredf, storydf, left_on='Iteration', right_on='Iteration', how='outer'))
    except:
        featuredf=pd.DataFrame()
        featuredf['Iteration'] = np.nan
        featuredf['Iteration'] = featuredf['Iteration'].astype('str')

    finaldf=(pd.merge(featuredf, storydf, left_on='Iteration', right_on='Iteration', how='outer'))
    
    #######################Task###############
    print("################task#########")
    taskdf=pd.DataFrame()
    taskdf['Iteration'] = np.nan
    taskdf['Iteration'] = taskdf['Iteration'].astype('str')
    try: 
        Task=(Agile.loc[Agile['WorkItemType'] == 'Task'])
        totaltask=Task.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
        df9=Task.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
        completedtasks=(df9.loc[df9['State'] == 'Closed'])
        completedtasks.rename(columns={'Totalstate':'Completed'}, inplace=True)
        completedtasks=completedtasks.drop(['State','WorkItemType'], axis = 1)
        df10=(pd.merge(totaltask, completedtasks, left_on='Iteration', right_on='Iteration', how='outer'))
        df10=df10.drop(['WorkItemType'], axis = 1)
        df11=df10.fillna(0)
        df11['Remaining'] = df11["Total"] - df11["Completed"] 
        df11.rename(columns={'Completed':'Task Completed','Total':'Total Tasks','Remaining':'Task Remaining'}, inplace=True)
      
        data=set(Story['workitemexternalid']).intersection(set(Task['itemexternalid']))
        data1=Task[Task['itemexternalid'].isin(list(data))]
        data2=data1.groupby(['Iteration','WorkItemType'], as_index=False)['EffortEstimated','EffortRemaining','EffortCompleted'].sum()
        data2=data2.drop(['WorkItemType'], axis = 1)
        data2.rename(columns={'EffortEstimated':'Task EffortEstimated','EffortRemaining':'Task EffortRemaining','EffortCompleted':'Task EffortCompleted'}, inplace=True)
        taskdf=(pd.merge(df11, data2, left_on='Iteration', right_on='Iteration', how='outer'))
        
    except:
        taskdf=pd.DataFrame()
        taskdf['Iteration'] = np.nan
        taskdf['Iteration'] = taskdf['Iteration'].astype('str')
    finaldf1=(pd.merge(finaldf, taskdf, left_on='Iteration', right_on='Iteration', how='outer'))
    
    ###############################Issue##############
    
    issuedf=pd.DataFrame()
    issuedf['Iteration'] = np.nan
    issuedf['Iteration'] = issuedf['Iteration'].astype('str')
    try:
        print("#############issue###########")
        Issue=(Agile.loc[Agile['WorkItemType'] == 'Issue'])
        totalissues=Issue.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
        df13=Issue.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
        completedissues=(df13.loc[df13['State'] == 'Closed'])
        completedissues.rename(columns={'Totalstate':'Completed'}, inplace=True)
        completedissues=completedissues.drop(['State','WorkItemType'], axis = 1)
        issuedf=(pd.merge(totalissues, completedissues, left_on='Iteration', right_on='Iteration', how='outer'))
        issuedf=issuedf.drop(['WorkItemType'], axis = 1)
        issuedf=issuedf.fillna(0)
        issuedf['Remaining'] = issuedf["Total"] - issuedf["Completed"] 
        issuedf.rename(columns={'Completed':'Issue Completed','Total':'Issue Total','Remaining':'Issue Remaining'}, inplace=True)
    except:
        issuedf=pd.DataFrame()
        issuedf['Iteration'] = np.nan
        issuedf['Iteration'] = issuedf['Iteration'].astype('str')
    finaldf2=(pd.merge(finaldf1, issuedf, left_on='Iteration', right_on='Iteration', how='outer'))
    
    #########EPIC###############
    epicdf=pd.DataFrame()
    epicdf['Iteration'] = np.nan
    epicdf['Iteration'] = epicdf['Iteration'].astype('str')
    try:
        print("##########epic##########")
        Epic=(Agile.loc[Agile['WorkItemType'] == 'Epic'])
        totalepics=Epic.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
        df15=Epic.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
        completedepics=(df15.loc[df15['State'] == 'Closed'])
        completedepics.rename(columns={'Totalstate':'Completed'}, inplace=True)
        completedepics=completedepics.drop(['State','WorkItemType'], axis = 1)
        epicdf=(pd.merge(totalepics, completedepics, left_on='Iteration', right_on='Iteration', how='outer'))
        epicdf=epicdf.drop(['WorkItemType'], axis = 1)
        epicdf=epicdf.fillna(0)
        epicdf['Remaining'] = epicdf["Total"] - epicdf["Completed"] 
        epicdf.rename(columns={'Completed':'Epic Completed','Total':'Epic Total','Remaining':'Epic Remaining'}, inplace=True)
    except:
        epicdf=pd.DataFrame()
        epicdf['Iteration'] = np.nan
        epicdf['Iteration'] = epicdf['Iteration'].astype('str')
    finaldf3=(pd.merge(finaldf2, epicdf, left_on='Iteration', right_on='Iteration', how='outer'))
    
    ###################Impediment########
    impedimentdf=pd.DataFrame()
    impedimentdf['Iteration'] = np.nan
    impedimentdf['Iteration'] = impedimentdf['Iteration'].astype('str')
    try:
        print("#####imp#####")
        Impediment=(Agile.loc[Agile['WorkItemType'] == 'Impediment'])
        totalimpediments=Impediment.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
        df17=Impediment.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
        comletedimpediments=(df17.loc[df17['State'] == 'Closed'])
        comletedimpediments.rename(columns={'Totalstate':'Completed'}, inplace=True)
        comletedimpediments=comletedimpediments.drop(['State','WorkItemType'], axis = 1)
        impedimentdf=(pd.merge(totalimpediments, comletedimpediments, left_on='Iteration', right_on='Iteration', how='outer'))
        impedimentdf=impedimentdf.drop(['WorkItemType'], axis = 1)
        impedimentdf=impedimentdf.fillna(0)
        impedimentdf['Remaining'] = impedimentdf["Total"] - impedimentdf["Completed"] 
        impedimentdf.rename(columns={'Completed':'Impediment Completed','Total':'Impediment Total','Remaining':'Impediment Remaining'}, inplace=True)
    except:
        impedimentdf=pd.DataFrame()
        impedimentdf['Iteration'] = np.nan
        impedimentdf['Iteration'] = impedimentdf['Iteration'].astype('str')
    finaldf4=(pd.merge(finaldf3, impedimentdf, left_on='Iteration', right_on='Iteration', how='outer'))
    
    #############Risk##############
    riskdf=pd.DataFrame()
    riskdf['Iteration'] = np.nan
    riskdf['Iteration'] = riskdf['Iteration'].astype('str')
    try:
        print("#####risk#####")
        Risk=(Agile.loc[Agile['WorkItemType'] == 'Risk'])
        totalrisks=Risk.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
        df19=Risk.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
        completedrisks=(df19.loc[df19['State'] == 'Closed'])
        completedrisks.rename(columns={'Totalstate':'Completed'}, inplace=True)
        completedrisks=completedrisks.drop(['State','WorkItemType'], axis = 1)
        riskdf=(pd.merge(totalrisks, completedrisks, left_on='Iteration', right_on='Iteration', how='outer'))
        riskdf=riskdf.drop(['WorkItemType'], axis = 1)
        riskdf=riskdf.fillna(0)
        riskdf['Remaining'] = riskdf["Total"] - riskdf["Completed"] 
        riskdf.rename(columns={'Completed':'Risk Completed','Total':'Risk Total','Remaining':'Risk Remaining'}, inplace=True)
    except:
        riskdf=pd.DataFrame()
        riskdf['Iteration'] = np.nan
        riskdf['Iteration'] = riskdf['Iteration'].astype('str')
    finaldf5=(pd.merge(finaldf4, riskdf, left_on='Iteration', right_on='Iteration', how='outer'))
    
    ###############################Defect#######################
    defectdf=pd.DataFrame()
    defectdf['Iteration'] = np.nan
    defectdf['Iteration'] = defectdf['Iteration'].astype('str')
    try:
        print("#####defect###")
        Defect=(Agile.loc[Agile['WorkItemType'] == 'Defect'])
        if Defect['Iteration'].isnull().all():
            print("Iteration column is null")
        else:
            Defect['StartOn']=Defect['StartOn'].replace(np.nan,'')
            Defect['CompletedOn']=Defect['CompletedOn'].replace(np.nan,'')
            Defect['createdon']=Defect['createdon'].replace(np.nan,'')
            Defect['Sprintstarton']=Defect['Sprintstarton'].replace(np.nan,'')
            Defect['Sprintendon']=Defect['Sprintendon'].replace(np.nan,'')
            Defect.CompletedOn=Defect.CompletedOn.str[:10]   #### use try and except here other wise through error
            Defect.createdon=Defect.createdon.str[:10]
            Defect.StartOn=Defect.StartOn.str[:10]
            Defect.Sprintstarton=Defect.Sprintstarton.str[:10]
            Defect.Sprintendon=Defect.Sprintendon.str[:10]
            
            totaldefects=Defect.groupby(['Iteration','WorkItemType']).size().reset_index(name="Total")
            defectstate=Defect.groupby(['Iteration','WorkItemType','State']).size().reset_index(name="Totalstate")
            
            completeddefects=(defectstate.loc[defectstate['State'] == 'Closed'])
            completeddefects.rename(columns={'Totalstate':'Completed'}, inplace=True)
            completeddefects=completeddefects.drop(['State','WorkItemType'], axis = 1)
            
            df22=(pd.merge(totaldefects, completeddefects, left_on='Iteration', right_on='Iteration', how='outer'))
            df22=df22.drop(['WorkItemType'], axis = 1)
            df23=df22.fillna(0)
            df23['Remaining'] = df23["Total"] - df23["Completed"] 
            
            df24=Defect.groupby(['Iteration','WorkItemType'], as_index=False)['EffortEstimated','EffortRemaining','EffortCompleted'].sum()
            df24=df24.drop(['WorkItemType'], axis = 1)
            df25=(pd.merge(df23, df24, left_on='Iteration', right_on='Iteration', how='outer'))
            df25.rename(columns={'Completed':'Total Defects','Total':'Defect Total','Remaining':'Defect Remaining','EffortEstimated':'Defect Estimated Efforts','EffortRemaining':'Defect EffortRemaining','EffortCompleted':'Defect EffortCompleted'}, inplace=True)
            
            date=Defect[['Iteration','createdon','StartOn','CompletedOn','WorkItemType','State']]
            date["createdon"] = pd.to_datetime(date["createdon"], format="%Y-%m-%d")
            date["StartOn"] = pd.to_datetime(date["StartOn"], format="%Y-%m-%d")
            date["CompletedOn"] = pd.to_datetime(date["CompletedOn"], format="%Y-%m-%d")
            date = date[date['Iteration'].notna()]
    
            date['average'] = ((date['CompletedOn'] - date['createdon']).dt.days)/2
            date1 = date[date['Iteration'].notna()]
            date1=date1.drop(['State','WorkItemType','createdon','StartOn','CompletedOn'], axis = 1)
            date2=date1.groupby(['Iteration'], as_index=False)['average'].sum()
            date3=Defect.groupby(['Iteration'], as_index=False)['createdon','StartOn'].min()
            date4=Defect.groupby(['Iteration'], as_index=False)['CompletedOn'].max()
            date5=[date2,date3,date4]
        
            date_merged = reduce(lambda  left,right: pd.merge(left,right,on=['Iteration'],how='outer'), date5)
        
            df26=(pd.merge(df25, date_merged, left_on='Iteration', right_on='Iteration', how='outer'))
            newdefects = defectstate[(defectstate['State']=='New')]
            newdefects.rename(columns={'Totalstate':'New Defects'}, inplace=True)
            newdefects=newdefects.drop(['State','WorkItemType'], axis = 1)
        
            activedefects = defectstate[(defectstate['State']=='Active')]
            activedefects.rename(columns={'Totalstate':'Active Defects'}, inplace=True)
            activedefects=activedefects.drop(['State','WorkItemType'], axis = 1)
        
            statedefect=(pd.merge(newdefects, activedefects, left_on='Iteration', right_on='Iteration', how='outer'))
            df27=(pd.merge(df26, statedefect, left_on='Iteration', right_on='Iteration', how='outer'))
            sprintdata=Defect[['Iteration','WorkItemType','createdon','Sprintstarton','Sprintendon']]
            presprintdate=sprintdata[(sprintdata['createdon'] < sprintdata['Sprintstarton'])]
            presprint=presprintdate.groupby(['Iteration','WorkItemType']).size().reset_index(name="Pre Sprint Defects")
            presprint=presprint.drop(['WorkItemType'], axis = 1)
            postsprintdate=sprintdata[(sprintdata['createdon'] > sprintdata['Sprintendon'])]
            postsprint=postsprintdate.groupby(['Iteration','WorkItemType']).size().reset_index(name="Post Sprint Defects")
            postsprint=postsprint.drop(['WorkItemType'], axis = 1)
            insprintdate=sprintdata[(sprintdata['Sprintstarton'] <= sprintdata['createdon']) & (sprintdata['Sprintstarton'] <= sprintdata['Sprintendon'])]
            insprint=insprintdate.groupby(['Iteration','WorkItemType']).size().reset_index(name="In Sprint Defects")
            insprint=insprint.drop(['WorkItemType'], axis = 1)
            allsprints=[presprint,postsprint,insprint]
            mergetest = reduce(lambda  left,right: pd.merge(left,right,on=['Iteration'],how='outer'), allsprints)
            mergetest = mergetest.replace(np.nan, 0)
            
            defectdf=(pd.merge(mergetest, df27, left_on='Iteration', right_on='Iteration', how='outer'))
            defectdf.rename(columns={'createdon':'First Defect Reported Date','StartOn':'Sprint Start Date','CompletedOn':'Sprint End Date '}, inplace=True)
    except:
        defectdf=pd.DataFrame()
        defectdf['Iteration'] = np.nan
        defectdf['Iteration'] = defectdf['Iteration'].astype('str')
    print("###finalbefore#####")
    finaldf6=(pd.merge(finaldf5, defectdf, left_on='Iteration', right_on='Iteration', how='outer'))
    #print("##finalafter####")
    #print(finaldf6)
    ##############################
    try:
        if Agile['Iteration'].isnull().all():
            print('Iteration column has no data')
            Agileaggdata=finaldf6
        else:
            if 'iterationstringuids' in Agile.columns:
                agilestatus = Agile[['iterationstringuids','Sprintstatus']]
                agilestatus = agilestatus[agilestatus['iterationstringuids'].notna()]
                agilestatus=agilestatus.drop_duplicates(subset='iterationstringuids', keep="last")
                finaldf6 = (pd.merge(finaldf6,agilestatus, left_on='Iteration', right_on='iterationstringuids', how='outer'))
                finaldf6['Iteration'] = finaldf6['Iteration'].replace('', np.nan, regex=True)
                finaldf6 = finaldf6[finaldf6['Iteration'].notna()]

            #print(Agile['Iteration'])
            print("###finalagg###")
            df28=Agile.groupby(['Iteration'], as_index=False)['StoryPointEstimated','BusinessValue'].sum()
            print("####finaldf28###")
            df29=Agile.groupby(['Iteration','Priority']).size().reset_index(name="Count of Critical and High items")
            df29=df29[df29['Priority'].isin(['Critical', 'High'])]
            df30=df29.groupby(['Iteration'], as_index=False)['Count of Critical and High items'].sum()
            df31=Agile.groupby(['Iteration','Priority','State']).size().reset_index(name="Total closed critical")
            df31 = df31[(df31['Priority']=='Critical') & (df31['State']=='Closed')]    
            user = Agile.groupby(by='Iteration', as_index=False).agg({'AssignedAtSourceToUser': pd.Series.nunique})
            user.rename(columns={'AssignedAtSourceToUser':'Team Size'}, inplace=True)
            team = Agile.groupby(by='Iteration', as_index=False).agg({'TeamArea': pd.Series.nunique})
            team.rename(columns={'TeamArea':'Unique Team'}, inplace=True)
            unique=(pd.merge(user, team, left_on='Iteration', right_on='Iteration', how='outer'))
            aggdata=(pd.merge(df28, df30, left_on='Iteration', right_on='Iteration', how='outer'))
            aggdata1=(pd.merge(aggdata, df31, left_on='Iteration', right_on='Iteration', how='outer'))
            aggdata2=(pd.merge(aggdata1, unique, left_on='Iteration', right_on='Iteration', how='outer'))
            aggdata2=aggdata2.drop(['State','Priority'], axis = 1)
            totalstorypoint=Agile[['Iteration','StoryPointEstimated','State']]
            totalstorypoint=(totalstorypoint.loc[totalstorypoint['State'] == 'Closed'])
            totalstorypoint=totalstorypoint.groupby(['Iteration'], as_index=False)['StoryPointEstimated'].sum()
            totalstorypoint.rename(columns={'StoryPointEstimated':'Total StoryPointCompleted'}, inplace=True)
            aggdata3=(pd.merge(aggdata2, totalstorypoint, left_on='Iteration', right_on='Iteration', how='outer'))
            aggdata3.rename(columns={'StoryPointEstimated':'Total Planned Story Points','BusinessValue':'Total BusinessValue',}, inplace=True)
            Agiledf=(pd.merge(finaldf6, aggdata3, left_on='Iteration', right_on='Iteration', how='outer'))
            name=Agile[['Iteration','Iterationname','Release','Releasename']]
            name = name[name['Iteration'].notna()]
            name1=name.drop_duplicates(subset='Iteration', keep="last")
            Agileaggdata=(pd.merge(name1,Agiledf, left_on='Iteration', right_on='Iteration', how='outer'))
    except:
        
        Agileaggdata=finaldf6
        
        
    Agileaggdata.rename(columns={'Iteration':'Sprintid','Iterationname':'Sprint Name'},inplace=True)
    return Agileaggdata


def getEntityArgs(cid,dcuid,entity,fromdate,todate):
    if entity != 'Iteration':
        entityArgs = {
            "ClientUID"            : cid,
            "DeliveryConstructUId" : dcuid,
            "EntityUId"            : eval(EntityConfig['WorkItem']['EntityUID']),
            "ColumnList"           : eval(EntityConfig['WorkItem']['RequiredColumns']),
            "WorkItemTypeUId"      : [val for key, val in eval(EntityConfig['CDMConfig']['AgileWorkItemTypes']).items() if entity == key][0],
            "RowStatusUId"         : eval(EntityConfig['CDMConfig']['rowstatusuid'])['Active'],
            "Displayname"          : eval(EntityConfig['WorkItem'][entity]),
            "PageNumber"           : "1",
            "TotalRecordCount"     : "0",
            "BatchSize"            : "5000",
            "FromDate"             : fromdate,
            "ToDate"               : todate}
    else:
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

def get_aggregated_data(cid,dcuid,fromdate,todate,config,parent,Incremental,auto_retrain,flag= None):
    auto_retrain = False
    lastDateDict= {}
    lastDateDict['Entity'] = {}
    MultiSourceDfs = {}
    global entityDfs 
    entityDfs = {}
    
    k = {
            "ClientUId": cid,
            "DeliveryConstructUId": dcuid
        }

    entityAccessToken,status_code = utils.getMetricAzureToken()
    for entity in  ['Story','Feature','Task','Issue','Epic','Impediment','Risk','Defect']:
        entityArgs = getEntityArgs(cid,dcuid,entity,fromdate,todate)
        agileAPI = EntityConfig['CDMConfig']['CDMAPI']
        agileAPI = agileAPI.format(urlencode(k))
        agileAPI = agileAPI.format(urlencode(k))
        utils.CallCdmAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
        print("call cdm")
    MultiSourceDfs['Entity'] = entityDfs
    for i in MultiSourceDfs['Entity']:
        if type(MultiSourceDfs['Entity'][i])!= str:
            print(MultiSourceDfs['Entity'][i].shape)
        else:
            print("No data")
    EntityMappingColumns = eval(config['CDMConfig']['EntityMappingColumns'])
    MultiSourceDfs,_ = utils.TransformEntities(MultiSourceDfs,cid,dcuid,EntityMappingColumns,parent,Incremental,auto_retrain,flag)
    print("after transform")
    entity = "Iteration"
    agileAPI = agileAPI.format(urlencode(k))
    entityArgs = getEntityArgs(cid,dcuid,entity,fromdate,todate)
    utils.IterationAPI(agileAPI,entityDfs,entity,auto_retrain,lastDateDict,entityArgs,entityAccessToken)
    iterationdf = entityDfs['Iteration']
    Defectdatadf = MultiSourceDfs['Entity']['Defect']
    if isinstance(iterationdf, pd.DataFrame) and isinstance(Defectdatadf, pd.DataFrame):
        iterationdf['iterationuid'] = iterationdf['iterationuid'].replace('',np.nan)
        itertiondata = iterationdf[['iterationuid','iterationstringuid','starton','endon']]
        itertiondata['iterationuid'] = itertiondata['iterationuid'].dropna()
        itertiondata=itertiondata.drop_duplicates(subset='iterationuid', keep="last")
        itertiondata.rename(columns={'starton':'Sprintstarton','endon':'Sprintendon'}, inplace=True)
        MultiSourceDfs['Entity']['Defect'] = (pd.merge(MultiSourceDfs['Entity']['Defect'], itertiondata, left_on='Iteration', right_on='iterationstringuid', how='left'))
        if "Iteration" in MultiSourceDfs['Entity']:
            print("yes")
            del MultiSourceDfs['Entity']["Iteration"]
    l=[]
    collection = MultiSourceDfs['Entity']
    columns1=['Iterationname','Releasename']
    for c in MultiSourceDfs['Entity']:
        if type(MultiSourceDfs['Entity'][c]) != str:
            MultiSourceDfs['Entity'][c]['WorkItemType'] = pd.Series(data = c,index = collection[c].index)
            l.append(MultiSourceDfs['Entity'][c])
    #print("l",l)
    if l!= []:
        Agiledata = pd.concat(l)
        print(Agiledata.columns)
        Agiledata.reset_index(drop = True,inplace = True)
        Agile  = pd.DataFrame(Agiledata)
        Agile = Agile.apply(pd.to_numeric, errors="ignore")
        columns2=Agile.columns
        duplicates  = list(set(columns1) - set(columns2))
        Agile['Iteration'] = Agile['Iteration'].astype('str')
        if len(duplicates)>0:
            dfnew=pd.DataFrame(columns=duplicates)
            dfnew = dfnew.fillna(0)
            Agile = pd.concat([Agile,dfnew])
        if isinstance(iterationdf, pd.DataFrame):
            itertiondata = iterationdf[['iterationstringuid','endon']]
            itertiondata.rename(columns={'iterationstringuid':'iterationstringuids','endon':'iterationendon'}, inplace=True)
            itertiondata["iterationendon"] = pd.to_datetime(itertiondata["iterationendon"])
            todate = datetime.date.today()
            todate= pd.to_datetime(todate)
            itertiondata['Sprintstatus'] = ['closed' if x < todate else 'active' for x in itertiondata['iterationendon']]
            itertiondata['iterationstringuids'] = itertiondata['iterationstringuids'].astype('str')
            Agile = (pd.merge(Agile,itertiondata, left_on='Iteration', right_on='iterationstringuids', how='outer'))

        #print('loop',Agile)
        #print(duplicates)
        #new=Agile[['Iteration','WorkItemType','StartOn']]
        #print("#####new##########",new)

        aggData = Agileaggdata_function(Agile)
    else:
        aggData = pd.DataFrame()
    
    
    return aggData
        
   


