# -*- coding: utf-8 -*-
"""
Created on Wed Nov 25 11:33:15 2020

@author: k.sudhakara.reddy
"""

# -*- coding: utf-8 -*-
import platform
from datetime import datetime
import pandas as pd
import requests
import numpy as np
import re
import json
import os,sys
import utils 
import GetPhoenixData
import EncryptData
import configparser, os
import file_encryptor
import datetime as dt


config = configparser.RawConfigParser()
conf_path = "/pythonconfig.ini"
configpath = str(os.getcwd()) + str(conf_path)

try:
    config.read(configpath)
except UnicodeDecodeError:
    print("Decrypting Config File : utils.py")
    config = file_encryptor.get_configparser_obj(configpath)

auth_type=config['auth_scheme']['authProvider']

def Clientnative(cid,dcuid):
    metadataAPI = config['ClientNative']['metadataAPI']
    entityuid=config['ClientNative']['entityuid']
        
    args = {
            "ClientUId" : cid,
            "DeliveryConstructUId" : dcuid,
            "EntityUId": entityuid,
            }
    if auth_type == 'AzureAD':
        tokenArgs={
                "grant_type": config['ClientNative']['grant_type'],
                "resource": config['ClientNative']['resource'],
                "client_id": config['ClientNative']['client_id'],
                "client_secret": config['ClientNative']['client_secret']
                }
        AdTokenUrl = config['ClientNative']['AdTokenUrl']
    
        token = requests.post(AdTokenUrl,data=tokenArgs,headers = {'Content-Type':'application/x-www-form-urlencoded'})
                   
        EntityAccessToken = token.json()['access_token']    
    
    clientuid = cid
    dcid = dcuid
    AppServiceUId = config['ClientNative']['AppServiceUId']
    if auth_type == 'AzureAD':
        Results = requests.post(metadataAPI,data=json.dumps(args),
                                                   headers={'Content-Type':'application/json',
                                                        'authorization': 'bearer {}'.format(EntityAccessToken),
                                                        'AppServiceUId':AppServiceUId},
                                                  params={"clientUID": clientuid, 
                                                           "deliveryConstructUId":dcid}
                                                       )
    elif auth_type == 'WindowsAuthProvider':
        Results = requests.post(metadataAPI,data=json.dumps(args),
                                                   headers={'Content-Type':'application/json',
                                                        'AppServiceUId':AppServiceUId},
                                                  params={"clientUID": clientuid, 
                                                           "deliveryConstructUId":dcid},
                                                  auth=HttpNegotiateAuth())
    
    guid=Results.json()['EntityPropertyValues'][30]['Values']
    x = {}
    clientnative=pd.DataFrame()
    for j in range(len(guid)):
        cols=['ProductPropertyValueUId', 'ProductPropertyValueDisplayName']
        for i in cols:
            x.update({i.lower():guid[j][i]})
        dfc = pd.DataFrame([x])
        clientnative=clientnative.append(dfc)
        clientnative.reset_index(drop = True,inplace = True)
    clientnative['productpropertyvalueuid']=[re.sub('[^a-zA-Z0-9]+', '', _) for _ in clientnative.productpropertyvalueuid]
    return clientnative

def AggregateRRPData(cid,dcuid,tempId,version):
    logger = utils.logger('Get', tempId)
    clientNativePhases = {"19AB9394836C451DB95F000000000018" : "Plan" , "19AB9394836C451DB95F000000000019" : "Analyze" , "19AB9394836C451DB95F000000000020" : "Design" , "19AB9394836C451DB95F000000000021" : "Build" , "19AB9394836C451DB95F000000000022" : "Component Test" , "19AB9394836C451DB95F000000000023" : "Assembly Test" , "19AB9394836C451DB95F000000000024" : "Product Test" , "19AB9394836C451DB95F000000000026" : "Deploy" , "19AB9394836C451DB95F000000000028" : "Detailed Technical Design" }
    utils.logger(logger, tempId, 'INFO', ('IngestPhoenixData '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    utils.UpdateProgress(tempId,version,"P",5,"InProgress")
    MultiSourceDfs = GetPhoenixData.getData(cid,dcuid,tempId)
    Iteration=MultiSourceDfs['Entity']['Iteration']
  
    defect = MultiSourceDfs['Entity']['Defect']

    if isinstance(Iteration, pd.DataFrame) and isinstance(defect, pd.DataFrame):   
        try:
            Iteration.rename(columns={'actualstarton':'ActualStartOn','actualendon':'ActualEndOn','PercentEffortCompleted':'percenteffortcompleted','EffortCompleted':'effortcompleted','EffortEstimated':'effortestimated'}, inplace=True)          
            Iteration.ActualStartOn=Iteration.ActualStartOn.str[:10] 
            Iteration.ActualEndOn=Iteration.ActualEndOn.str[:10]
            Iteration.endon=Iteration.endon.str[:10]
           
            Iteration['phasetypeuid'] =  Iteration['phasetypeuid'].replace('',np.nan)
           
            x1          =   Iteration[Iteration.phasetypeuid.isin(list(Iteration.phasetypeuid.dropna().unique()))]
        
            x3          =   x1[~x1.iterationassociations_iterationtypeuid.isna()]
         
            empty_cols = [col for col in x3.columns if x3[col].dropna().empty]
         
            x3.drop(columns = empty_cols ,inplace = True)
           
            releases    =   list(x3[x3.iterationassociations_iterationtypeuid.isin(["00200390001000000000000000000000"])]['itemassociateduid'].unique())
      
        
            ##NEWWWWWWWWWWWWWWW
            cols = ['Release State','Release Name','Overall Effort (Hrs)']
            Effort = pd.DataFrame(columns = cols,index = releases)
            ##NEWWWWWWWWWWWWWWW 
            
            
            k = list(x3.name.unique())
            effortphase=k
            for name in k:
                 Effort[name] = pd.Series(data = None)
                 
            for rel in releases:
                x4 = Iteration[Iteration.iterationuid.isin([rel])]
                Effort['Release Name'][rel] = list(x4.name)[0]
                x5 = x3[x3.itemassociateduid.isin([rel])]
                if (list(x4.ActualEndOn.astype(str))[0] == ''):
                     Effort['Release State'][rel]  = "Current"
                     for phase in x5.name.unique():
                        x6 = x5.loc[x5.name == phase]
                        t = list(x6.PercentEffortCompleted.dropna().unique())
                        if t == 100:
                            Effort[phase][rel] = float(list(x6.EffortCompleted.unique())[0])/3600
                        elif len(t) == 0:
                            Effort[phase][rel] = float(list(x6.EffortEstimated.unique())[0])/3600
                        else:
                            Effort[phase][rel] = float(list(x6.EffortEstimated.unique())[0])/3600
                else:
                     Effort['Release State'][rel]  = "Past"
                     for phase in x5.name.unique():
                        x6 = x5.loc[x5.name == phase]
                        t = list(x6.PercentEffortCompleted.dropna().unique())
                        Effort[phase][rel] = float(list(x6.EffortCompleted.unique())[0])/3600
            Effort = Effort.fillna(0)
            for col in Effort[effortphase]:
                Effort['Overall Effort (Hrs)']= Effort['Overall Effort (Hrs)']+Effort[col]
                
            ##NEWWWWWWWWWWWWWWW 
            t1 = list(clientNativePhases.values()) + cols
            t2 = set(t1).intersection(Effort.columns)
            Effort = Effort[list(t2)]
            ##NEWWWWWWWWWWWWWWW 
            
            
        
    ########Schedule#################
            utils.UpdateProgress(tempId,version,"P",10,"InProgress") 
    
    
            ##NEWWWWWWWWWWWWWWW
            cols1 =['Release State','Release Name','Release Start Date (dd/mm/yyyy)','Release End Date (dd/mm/yyyy)','Overall Schedule (Days)']
            schedule = pd.DataFrame(columns = cols1,index = releases)
            ##NEWWWWWWWWWWWWWWW 
            
            x3["ActualStartOn"] = x3["ActualStartOn"].replace('',np.nan)
            x3["ActualStartOn"] = x3["ActualStartOn"].replace('',np.nan)
            x3["ActualStartOn"] = pd.to_datetime(x3["ActualStartOn"])
            x3["ActualEndOn"] = pd.to_datetime(x3["ActualEndOn"])
            x3["ActualStartOn"] = x3["ActualStartOn"].dt.strftime("%Y-%m-%d")
            x3["ActualEndOn"] = x3["ActualEndOn"].dt.strftime("%Y-%m-%d")
            x3["ActualStartOn"] = pd.to_datetime(x3["ActualStartOn"], format="%Y-%m-%d")
            x3["ActualEndOn"] = pd.to_datetime(x3["ActualEndOn"], format="%Y-%m-%d")
            x3['Total'] = ((x3['ActualEndOn'] - x3['ActualStartOn']).dt.days)
            
            k = list(x3.name.unique())
    
            for name in k:
                 schedule[name] = pd.Series(data = None)
                 
            for rel in releases:
                x4 = Iteration[Iteration.iterationuid.isin([rel])]
                schedule['Release Name'][rel] = list(x4.name)[0]
                x5 = x3[x3.itemassociateduid.isin([rel])]
                if (list(x4.ActualEndOn.astype(str))[0] == ''):
                    print("actual")
                    print(x4.ActualEndOn)
                    #print(x4.endon)
                    schedule['Release State'][rel]  = "Current"
                    #data['Release Start'][rel] = (list(x4.ActualStartOn.astype(str))[0])
                    schedule['Release Start Date (dd/mm/yyyy)'][rel] = (list(x4.ActualStartOn.unique())[0])
                    schedule['Release End Date (dd/mm/yyyy)'][rel] = (list(x4.endon.unique())[0])
                    for phase in x5.name.unique():
                        x6 = x5.loc[x5.name == phase]
                        schedule[phase][rel] = float(list(x6.Total.unique())[0])
                    
                else:
                    print("hi")
                    schedule['Release State'][rel]  = "Past"
                    schedule['Release Start Date (dd/mm/yyyy)'][rel] = (list(x4.ActualStartOn.unique())[0])
                    schedule['Release End Date (dd/mm/yyyy)'][rel] = (list(x4.ActualEndOn.unique())[0])
                    for phase in x5.name.unique():
                        x6 = x5.loc[x5.name == phase]
                        schedule[phase][rel] = float(list(x6.Total.unique())[0])
            
            schedule = schedule.fillna(0)
            schedule['Release Start Date (dd/mm/yyyy)']=schedule['Release Start Date (dd/mm/yyyy)'].replace('',np.nan)
            schedule['Release Start Date (dd/mm/yyyy)']=schedule['Release Start Date (dd/mm/yyyy)'].replace(0,np.nan)
            schedule['Release End Date (dd/mm/yyyy)'] = schedule['Release End Date (dd/mm/yyyy)'].replace('',np.nan)
            schedule['Release End Date (dd/mm/yyyy)'] = schedule['Release End Date (dd/mm/yyyy)'].replace(0,np.nan)           
            schedule['Overall Schedule (Days)'] = ((schedule['Release End Date (dd/mm/yyyy)'].astype('datetime64[ns]') - schedule['Release Start Date (dd/mm/yyyy)'].astype('datetime64[ns]')).dt.days)
           
            
            ##NEWWWWWWWWWWWWWWW 
            t1 = list(clientNativePhases.values()) + cols1
            t2 = set(t1).intersection(schedule.columns)
            schedule = schedule[list(t2)]
            ##NEWWWWWWWWWWWWWWW 
            
            #schedule = schedule.fillna(0)
      
        #########Defect##########
            utils.UpdateProgress(tempId,version,"P",20,"InProgress")
            defect = MultiSourceDfs['Entity']['Defect']
        
                
            ##NEWWWWWWWWWWWWWWW
            cols = ['Release State','Release Name','Overall Defect']
            Defect = pd.DataFrame(columns = cols,index = releases)
            ##NEWWWWWWWWWWWWWWW 
        
            defect["phasedetecteduid"].replace(clientNativePhases, inplace=True)
            l = list(defect.phasedetecteduid.dropna().unique())
            x9=defect[defect['itemassociateduid'].isin(releases)]
            defect = defect.replace('',np.nan)          #defectphase = l
            #defect=defect.fillna(0)
            for name in l:
                #print(name)
                Defect[name] = pd.Series(data = None)
            defect=defect.fillna(0) 
            for rel in releases:
                x4 = Iteration[Iteration.iterationuid.isin([rel])]
                x5 = defect[defect.itemassociateduid.isin([rel])]
                x5.fillna(0)
                Defect['Release Name'][rel] = list(x4.name)[0]
                if (list(x4.ActualEndOn.astype(str))[0] == ''):
                    Defect['Release State'][rel]  = "Current"
                    #data[phase][rel] = (list(group.phasedetecteduid.unique()))[0]
                    for item in x9.itemassociateduid:
                        if item == rel:
                            Defect['Overall Defect'][rel] = (defect.itemassociateduid == item).sum()
                    for phase in x5.phasedetecteduid.unique():
                        x6 = x5.loc[x5.phasedetecteduid == phase]
                        if phase != 0:
                            Defect[phase][rel] = len(list(x6[x6.itemassociateduid.isin([rel])]['phasedetecteduid']))
            
            
                else:
                     Defect['Release State'][rel]  = "Past"
                     for item in x9.itemassociateduid:
                         if item == rel:
                             Defect['Overall Defect'][rel] = (defect.itemassociateduid == item).sum()
                     for phase in x5.phasedetecteduid.unique():
                         x6 = x5.loc[x5.phasedetecteduid == phase]
                         if phase != 0:
                             Defect[phase][rel] = len(list(x6[x6.itemassociateduid.isin([rel])]['phasedetecteduid']))
                if 0 in Defect.columns:
                    del Defect[0]
                    
                    
                ##NEWWWWWWWWWWWWWWW 
                t1 = list(clientNativePhases.values()) + cols
                t2 = set(t1).intersection(Defect.columns)
                Defect = Defect[list(t2)]
                ##NEWWWWWWWWWWWWWWW 
                    
            
        ############Teamsize########
        #deliverable=Deliverable
            utils.UpdateProgress(tempId,version,"P",30,"InProgress")
            print("deliverable Started.................................................")
            defect = MultiSourceDfs['Entity']['Defect']
            deliverable = MultiSourceDfs['Entity']['Deliverable']
            try:
                d9 = deliverable[deliverable['itemassociateduid'].isin(releases)]
                d9['assignedatsourcetouser'] = d9['assignedatsourcetouser'].replace(np.nan,0)
            except:
                deliverable = pd.DataFrame()
                deliverable['itemassociateduid'] = np.nan
                deliverable['assignedatsourcetouser'] = np.nan
                d9 = deliverable[deliverable['itemassociateduid'].isin(releases)]
                d9['assignedatsourcetouser'] = d9['assignedatsourcetouser'].replace(np.nan,0)
                
    
      
    #        try:
    #            clientnative=Clientnative(cid,dcuid)
    #            if clientnative.empty:
    #                print("Getting clientnative error")
    #            else:
    #                x15=list(defect.phasedetecteduid.dropna().unique())
    #                clientnative['productpropertyvalueuid'] = clientnative['productpropertyvalueuid'].str.upper()
    #                k=clientnative[clientnative['productpropertyvalueuid'].isin(x15)]
    #                d=pd.Series(k.productpropertyvaluedisplayname.values,index=k.productpropertyvalueuid).to_dict()
                
                
            ##NEWWWWWWWWWWWWWWW
            cols = ['Release State','Release Name','Overall Team Size']
            Teamsize = pd.DataFrame(columns = cols,index = releases)
            ##NEWWWWWWWWWWWWWWW 
            
            
            defect["phasedetecteduid"].replace(clientNativePhases, inplace=True)
            defect = defect.replace('',np.nan)
            p = list(defect.phasedetecteduid.dropna().unique())
            x9=defect[defect['itemassociateduid'].isin(releases)]
            x9['assignedatsourcetouser']=x9['assignedatsourcetouser'].replace(np.nan,0)
            #d9=deliverable[deliverable['itemassociateduid'].isin(releases)]
            #d9['assignedatsourcetouser']=d9['assignedatsourcetouser'].replace(np.nan,0)
           # deliverable = deliverable.replace('',np.nan)
            for name in p:
                Teamsize[name] = pd.Series(data = None)
            defect = defect.fillna(0)
            for rel in releases:
                x4 = Iteration[Iteration.iterationuid.isin([rel])]
                x5 = defect[defect.itemassociateduid.isin([rel])]
                x5.fillna(0)
                Teamsize['Release Name'][rel] = list(x4.name)[0]
                if (list(x4.ActualEndOn.astype(str))[0] == ''):
                    Teamsize['Release State'][rel]  = "Current"
                    for item in x9.itemassociateduid:
                        if item == rel:
                            Teamsize['Overall Team Size'][rel] = len(set([i for i in list(x9[x9.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0]+[i for i in list(d9[d9.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0]))
                            #Teamsize['Overall Team Size'][rel] = len(set([i for i in list(x9[x9.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0]))
    
                    for phase in x5.phasedetecteduid.unique():
                        x6 = x5.loc[x5.phasedetecteduid == phase]
                        if phase != 0:
                            #Teamsize[phase][rel] = len(list(x6[x6.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()))
                            Teamsize[phase][rel] = len([i for i in list(x6[x6.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0])
                else:
                     Teamsize['Release State'][rel]  = "Past"
                     for item in x9.itemassociateduid:
                         if item == rel:
                             #Teamsize['Total'][rel] = len(list(x9[x9.itemassociateduid.isin([item])]['assignedatsourcetouser'].unique()))
                             Teamsize['Overall Team Size'][rel] = len(set([i for i in list(x9[x9.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0]+[i for i in list(d9[d9.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0]))
                             #Teamsize['Overall Team Size'][rel] = len(set([i for i in list(x9[x9.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0]))
    
                     for phase in x5.phasedetecteduid.unique():
                         x6 = x5.loc[x5.phasedetecteduid == phase]
                         if phase != 0:
                             #Teamsize[phase][rel] = len(list(x6[x6.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()))
                             Teamsize[phase][rel] = len([i for i in list(x6[x6.itemassociateduid.isin([rel])]['assignedatsourcetouser'].unique()) if i != 0])
                if 0 in Teamsize.columns:
                    del Teamsize[0]
                    
                ##NEWWWWWWWWWWWWWWW 
                t1 = list(clientNativePhases.values()) + cols
                t2 = set(t1).intersection(Teamsize.columns)
                Teamsize = Teamsize[list(t2)]
                ##NEWWWWWWWWWWWWWWW
                    
    #        except Exception as e:
    #            print(e)
    #            Teamsize = pd.DataFrame()
            utils.UpdateProgress(tempId,version,"P",40,"InProgress")
            
        except Exception as e:
            print(e)
            print("Update  EEE status...................")
            utils.UpdateProgress(tempId,version,"E",0, "Data is not availble")
        for rel in Effort.index:
                if Effort.loc[rel]['Release State'] == 'Past':
                  
                    
                    k2 = Effort.loc[rel]
                    b = ['Release Name', 'Release State','Overall Effort (Hrs)']
                    k2 =  k2.drop(b)
                    
                    k3 = Teamsize.loc[rel]
                    c = ['Release Name', 'Release State','Overall Team Size']
                    k3 =  k3.drop(c)
                    
                    k4 = schedule.loc[rel]
                    d = ['Overall Schedule (Days)','Release Name','Release State', 'Release Start Date (dd/mm/yyyy)','Release End Date (dd/mm/yyyy)']
                    k4 =  k4.drop(d)   
                    
                    k = pd.concat([k2,k3,k4])    
                    k.fillna(0,inplace = True)        
                        
                    if (k!=0).astype(int).sum() < 9:
                        Defect.drop([rel],inplace = True)
                        Effort.drop([rel],inplace = True)
                        Teamsize.drop([rel],inplace = True)
                        schedule.drop([rel],inplace = True)
        print("deliverable Started................................................")
        try:
            schedulepast = schedule.loc[schedule['Release State'].isin(["Past"])] 
            schedulecurrent = schedule.loc[schedule['Release State'].isin(["Current"])]      
            schedulepast = schedulepast.sort_values(['Release State', 'Release End Date (dd/mm/yyyy)'], ascending=[True, True])
            schedulepast = schedulepast.sort_values(['Release State', 'Release End Date (dd/mm/yyyy)'], ascending=[True, True])
            schedulepast = schedulepast.tail(19)
            schedule = pd.concat([schedulepast,schedulecurrent])
            schedule = schedule.sort_values(['Release Name'], ascending=[True])
            releaseid=list(schedule.index)
            Defect=Defect[Defect.index.isin(releaseid)]
            Defect = Defect.sort_values(['Release Name'], ascending=[True])
            Effort=Effort[Effort.index.isin(releaseid)]
            Effort = Effort.sort_values(['Release Name'], ascending=[True])
            Teamsize=Teamsize[Teamsize.index.isin(releaseid)]
            Teamsize = Teamsize.sort_values(['Release Name'], ascending=[True])
        except Exception as e:
            print(e)
            utils.UpdateProgress(tempId,version,"E",0,str(e.args))  
            
       
            
            
        tEffort = Effort[Effort['Release State'].isin(['Past'])]
        tTeamsize = Teamsize[Teamsize['Release State'].isin(['Past'])]
        tschedule = schedule[schedule['Release State'].isin(['Past'])]
        
        phasesDetails = list(clientNativePhases.values())
        
        for p in phasesDetails:
            if p in tEffort.columns:
                 if round((tEffort[p].fillna(0) == 0).sum()/len(tEffort[p]),2) > .25:
                   
                    Effort.drop(columns = [p],errors = 'ignore',inplace = True)
                    Teamsize.drop(columns = [p],errors = 'ignore',inplace = True)
                    schedule.drop(columns = [p],errors = 'ignore',inplace = True)
            elif p in tTeamsize.columns:
                 if round((tTeamsize[p].fillna(0) == 0).sum()/len(tTeamsize[p]),2) > .25:
                  
                    Effort.drop(columns = [p],errors = 'ignore',inplace = True)
                    Teamsize.drop(columns = [p],errors = 'ignore',inplace = True)
                    schedule.drop(columns = [p],errors = 'ignore',inplace = True)
            elif p in tschedule.columns:
                 if round((tschedule[p].fillna(0) == 0).sum()/len(tschedule[p]),2) > .25:
                   
                    Effort.drop(columns = [p],errors = 'ignore',inplace = True)
                    Teamsize.drop(columns = [p],errors = 'ignore',inplace = True)
                    schedule.drop(columns = [p],errors = 'ignore',inplace = True)
            else:
                pass
        try:
            _phases =set(['Release State','Release Name','Release Start Date (dd/mm/yyyy)',
                    'Release End Date (dd/mm/yyyy)','Overall Schedule (Days)',
                    'Overall Effort (Hrs)','Overall Defect','Overall Team Size'])
            
            commonpases = set(list(schedule.columns) + list(Teamsize.columns) +list(Effort.columns) +list(Defect.columns))
            commonpases = list(commonpases.difference(_phases))
        except Exception as e:
            utils.UpdateProgress(tempId,version,"E",0,str(e.args))
            print(e)
        
    
        try:
            utils.UpdateProgress(tempId,version,"P",50,"InProgress")
            schedule["Release Start Date (dd/mm/yyyy)"] = schedule["Release Start Date (dd/mm/yyyy)"].replace(0,np.nan)
            schedule["Release End Date (dd/mm/yyyy)"] = schedule["Release End Date (dd/mm/yyyy)"].replace(0,np.nan)
            schedule["Release Start Date (dd/mm/yyyy)"] = pd.to_datetime(schedule["Release Start Date (dd/mm/yyyy)"],errors = 'coerce', format="%Y-%m-%d")
            schedule["Release End Date (dd/mm/yyyy)"] = pd.to_datetime(schedule["Release End Date (dd/mm/yyyy)"],errors = 'coerce', format="%Y-%m-%d")
            schedule.dropna(subset = ['Release Start Date (dd/mm/yyyy)','Release End Date (dd/mm/yyyy)'],inplace = True)
            relid= list(schedule.index)
            Defect=Defect[Defect.index.isin(relid)]
            Effort=Effort[Effort.index.isin(relid)]
            Teamsize=Teamsize[Teamsize.index.isin(relid)]
            schedule["Release Start Date (dd/mm/yyyy)"] = schedule["Release Start Date (dd/mm/yyyy)"].astype(str)
            schedule["Release End Date (dd/mm/yyyy)"] = schedule["Release End Date (dd/mm/yyyy)"].astype(str)
        except Exception as e:
            utils.logger(logger, tempId, 'INFO', ('IngestPhoenixData '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
            utils.UpdateProgress(tempId,version,"E",0,str(e.args))
            print(e)
          
        try:
            p4 = ['Overall Schedule (Days)','Release Name','Release End Date (dd/mm/yyyy)','Release Start Date (dd/mm/yyyy)']
            p3 = ['Overall Team Size']
            p2 = ['Overall Defect']
            p1 = ['Overall Effort (Hrs)']
            
            glist1 = set(Effort.columns).intersection(schedule.columns)
            glist2 = glist1.intersection(Defect.columns)
            glist3 = list(glist2.intersection(Teamsize.columns))
            
            Effort   = Effort[glist3 + p1]
            Defect   = Defect[glist3 + p2]
            Teamsize = Teamsize[glist3 + p3]
            schedule = schedule[glist3 + p4]
            
            Effortdf = {}
            Defectdf={}
            Teamsizedf={}
            scheduledf={}
            relname = list(Effort["Release Name"])
            relState = list(Effort["Release State"])
            rel = list(Effort["Release Name"])
            relState = list(Effort["Release State"])
            for col in Effort.columns:
                if col not in ["Release State","Release Name"]:
                    Effortdf[col] = list(Effort[col].fillna(0))
            for col in Defect.columns:
                if col not in ["Release State","Release Name"]:
                    Defectdf[col] = list(Defect[col].fillna(0))
            for col in Teamsize.columns:
                 if col not in ["Release State","Release Name"]:
                     Teamsizedf[col] = list(Teamsize[col].fillna(0))
            for col in schedule.columns:
                 if col not in ["Release State","Release Name"]:
                     scheduledf[col] = list(schedule[col].fillna(0))
            
           
            _phases =set(['Release State','Release Name','Release Start Date (dd/mm/yyyy)',
                        'Release End Date (dd/mm/yyyy)','Overall Schedule (Days)',
                        'Overall Effort (Hrs)','Overall Defect','Overall Team Size'])
                
            commonpases = set(list(schedule.columns) + list(Teamsize.columns) +list(Effort.columns) +list(Defect.columns))
            commonpases = list(commonpases.difference(_phases))
           
            
            print("Going to utils.......................................")
            y1=(list(commonpases))
            y2 = (dict.fromkeys(y1, "True"))
            utils.UpdateProgress(tempId,version,"P",60,"InProgress")
            
            y3={
                "Effort (Hrs)" : Effortdf,
                "Team Size" : Teamsizedf,
                "Schedule (Days)" : scheduledf,
                "Defect" : Defectdf,
                "Release State" : relState,
                "Release Name" : relname
                
                }
            y3 = EncryptData.EncryptIt(str(y3))
            utils.UpdateProgress(tempId,version,"P",70,"InProgress")
            utils.UpdateCDMData(tempId,version,y1,y2,y3)
            utils.logger(logger, tempId, 'INFO', ('IngestPhoenixData '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
            utils.UpdateProgress(tempId,version,"C",100,"Completed")
        except Exception as e:
            utils.logger(logger, tempId, 'INFO', ('IngestPhoenixData '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
            utils.UpdateProgress(tempId,version,"E",0,str(e.args))
        return
    else:
        print("Inside Elseeeeeeeeeeeeeeeeeeee")
        if type(Iteration) == str:
            print("updated E status")
            utils.logger(logger, tempId, 'INFO', ('IngestPhoenixData '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
            utils.UpdateProgress(tempId,version,"E",0,"Phoenix data fabric : Iteration" +str(Iteration).lower())
            sys.exit(str(Iteration))
            print("Done")
   
        if type(defect) == str:
            print("updated  defect E status")
            utils.logger(logger, tempId, 'INFO', ('IngestPhoenixData '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
            utils.UpdateProgress(tempId,version,"E",0,"Phoenix data fabric : Defect "+str(defect).lower())
            sys.exit(str(defect))
            print("Done")
    


if __name__ == '__main__':  
    print("hereeeee6")
    cid                  = sys.argv[1]
    dcuid                = sys.argv[2]
    tempId               = sys.argv[3]
    version              = sys.argv[4]
    print("Herethree33")
    try:
        AggregateRRPData(cid,dcuid,tempId,version)
    except Exception as ex:
        print(str(ex))    
    
    
    

    

    
    
        
    


        

    

