import pandas as pd
import numpy as np
from usecase import usecase_one,usecase_two,usecase_three
from scipy.stats import norm
from utils import open_dbconn
from pymongo import MongoClient
import base64
import json
import EncryptData
import utils
from datetime import datetime

#TemplateID= "0a48bdd5-813c-4ab9-9d58-a81050b7ab41"
#SimulationID='2e21323f-ab1a-4908-bc84-99855efb0832'

def whatIf(TemplateID,SimulationID,inputs):
    logger = utils.logger('Get', TemplateID)
    utils.logger(logger, TemplateID, 'INFO', ('whatif started'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    dbconn,dbcollection = open_dbconn("SimulationResults")
    data_json = dbcollection.find({"TemplateID" :TemplateID,"SimulationID":SimulationID}) 
    dbconn.close()
#    client = MongoClient('localhost',27017)
#    db = client.get_database(name = "inGrainDB")
#    dbcollection = db["SimulationResults"]
#    data_json = dbcollection.find({"TemplateID" :TemplateID,"SimulationID":SimulationID})    
#    client.close()
    k = list(data_json)
    input_dict= {
            "TargetCertainty":float(inputs["TargetCertainty"]),
            "TargetVariable" : float(inputs["TargetVariable"]),
            "Influencers":inputs["Influencers"]
                 }

    ChangedField =inputs["ChangedField"]
    encryption = k[0].get("isDBEncryption")
    try:
        if input_dict["Influencers"]['Team Size'] == None:
            del input_dict["Influencers"]['Team Size']
    except Exception as e:
        error_encounterd = str(e.args[0])
    target_distribution = k[0].get("Target_Distribution")
    if encryption==True:
        t = base64.b64decode(target_distribution)
        target_distribution = eval(EncryptData.DescryptIt(t))
    model_params = k[0].get("ModelParams")
    if encryption==True:
        t = base64.b64decode(model_params)     
        model_params = eval(EncryptData.DescryptIt(t))
        
    ProblemType =k[0].get("ProblemType")
    targetvariable = k[0].get("TargetColumn")
    currentInfluencers = k[0].get("CurrentInfluencers")
    currentTarget = k[0].get("TargetVariable")
#    currentCertainty = k[0].get("TargetCertainty")
    if encryption==True:
        currentInfluencers_en = base64.b64decode(currentInfluencers)
        currentInfluencers = eval(EncryptData.DescryptIt(currentInfluencers_en))
        currentTarget_en = base64.b64decode(currentTarget)
        currentTarget = eval(EncryptData.DescryptIt(currentTarget_en))
    
    current_dict= {
            "TargetVariable":float(currentTarget),
            "Influencers":currentInfluencers
            }
    
    for i in list(current_dict.keys()):
        if i =="Influencers" :
            for j in list(input_dict["Influencers"].keys()):
                if j!=targetvariable:
                    input_dict["Influencers"][j] = int(round(float(current_dict["Influencers"][j])+(float(current_dict["Influencers"][j])*float(input_dict["Influencers"][j]))/100))
        else:
            input_dict[i] = float(current_dict[i])+(float(current_dict[i])*float(input_dict[i]))/100
    
    if((ChangedField) == "TargetCertainty") or ((ChangedField)==targetvariable):
        input_dict.update({"Influencers":currentInfluencers})

    if (ProblemType=="ADSP" or ProblemType =="RRP"):
        temp,cols = {},[]
        ipdict = input_dict.copy()
        for i in input_dict["Influencers"]:
            val = input_dict["Influencers"][i]
            i = "Overall "+i 
            temp.update({i:val})
            cols.append(i)
        inf_dist = k[0].get("InfluencerDistributions")
        ##############
        if encryption==True:
            t = base64.b64decode(inf_dist)     
            inf_dist = eval(EncryptData.DescryptIt(t))
        ##############
        input_dict.update({"Influencers":temp})
        temp_params = []
        for i in cols:
            temp_params.append(model_params[1][i])
        model_params = [model_params[0],temp_params]
        
        if(("Overall "+ChangedField) in list(input_dict["Influencers"].keys())):
            whatif_results = usecase_one(ipdict,model_params,target_distribution,ProblemType)
        elif((ChangedField) == "TargetCertainty"):
            cols = [i[8:] for i in cols]
            whatif_results = usecase_two(input_dict,model_params,cols,target_distribution,ProblemType)
        elif((ChangedField)==targetvariable):
            cols = [i[8:] for i in cols]
            whatif_results = usecase_three(input_dict,model_params,cols,target_distribution,ProblemType)
        certaintyvalues = {}
    
        for i in inf_dist:
            c = round(norm.cdf(whatif_results["Influencers"][i],inf_dist[i]["mean"],inf_dist[i]["standard_deviation"])*100,2)
            certaintyvalues.update({i:c})
#        whatif_results.update({"CertaintyValues":certaintyvalues})
        whatif_results.update({"TargetColumn":k[0].get("TargetColumn")})
        if str(whatif_results["TargetCertainty"])=="nan":
            whatif_results["TargetCertainty"] = 100
        if (whatif_results["TargetCertainty"] >= 85):
            Observation = "With the "+ str(whatif_results["TargetVariable"])+" defects closed , the certainty of completing the release is "+str(int(whatif_results["TargetCertainty"]))+ "%. There is "+str(100-int(whatif_results["TargetCertainty"]))+"% risk with respect to delivered defects. Monitor the critical subprocesses identified through Sensitivity Analysis."
        else:
            Observation = "With the "+ str(whatif_results["TargetVariable"])+" defects closed , the certainty of completing the release is "+str(int(whatif_results["TargetCertainty"]))+ "%. There is "+str(100-int(whatif_results["TargetCertainty"]))+"% risk with respect to delivered defects. Perform Scenario Analysis to mitigate the risk identified as the certainty is <85%."
        whatif_results.update({"Observation":Observation})
        
        # Creating the flags for the changed values and and finding percentage change below
        influencer_flags = {}
        percent_change = {}
        
        
            
    elif(ProblemType=="Generic"):
        default_cols =k[0].get("DefaultColumns")

        if encryption==True:
            t = base64.b64decode(default_cols)     
            default_cols = eval(EncryptData.DescryptIt(t))

        a = {"Influencers":{}}
        cols = []
        if (len(default_cols)>0):
            cols = list(default_cols.keys())
            a.update({"Influencers" : default_cols})
        for i in input_dict["Influencers"]:
            a["Influencers"].update({i : round(input_dict["Influencers"][i],2)})
        input_dict.update({"Influencers":a["Influencers"]})
        inf_cols = list(input_dict["Influencers"].keys())
        ##
        temp_params = []
        for i in inf_cols:
            temp_params.append(model_params[1][i])
        model_params = [model_params[0],temp_params]
        ##
        if(ChangedField in list(input_dict["Influencers"].keys())):
            whatif_results = usecase_one(input_dict,model_params,target_distribution,ProblemType)
        elif(ChangedField == "TargetCertainty"):
            whatif_results = usecase_two(input_dict,model_params,inf_cols,target_distribution,ProblemType)
        elif(ChangedField==targetvariable):
            whatif_results = usecase_three(input_dict,model_params,inf_cols,target_distribution,ProblemType)
        temp_dict = {}
        for i in whatif_results["Influencers"]:
            if i not in cols:
                temp_dict.update({i:whatif_results["Influencers"][i]})
        whatif_results.update({"Influencers":temp_dict})
        whatif_results.update({"CertaintyValues":{}})
        whatif_results.update({"TargetColumn":k[0].get("TargetColumn")})
        whatif_results.update({"Observation":{}})
        influencer_flags = {}
        percent_change = {}
#        for i in list(current_dict.keys()):
#            if current_dict[i]>whatif_results[i]:
#                influencer_flags.update({i:1}) # for decreasing from actual value
#                percent=((whatif_results[i]-current_dict[i])/current_dict[i])*100
#                percent_change.update({i:round(percent,2)})
#            elif current_dict[i]<whatif_results[i]:
#                influencer_flags.update({i:2}) # for increasing from actual value
#                percent=((whatif_results[i]-current_dict[i])/current_dict[i])*100
#                percent_change.update({i:round(percent,2)})
#            else:
#                influencer_flags.update({i:0})
#                percent_change.update({i:0})
#        whatif_results.update({"IncrementFlags": influencer_flags})
#        whatif_results.update({"PercentChange":percent_change})
#        output = whatif_results
    for i in list(current_dict.keys()):
        try:
            if i =="Influencers":
                for j in list(current_dict["Influencers"].keys()):
                    if current_dict[i][j]>whatif_results[i][j]:
                        influencer_flags.update({j:1}) # for decreasing from actual value
                        percent=((whatif_results[i][j]-float(current_dict[i][j]))/float(current_dict[i][j]))*100
                        percent_change.update({j:round(percent,2)})
                    elif current_dict[i][j]<whatif_results[i][j]:
                        influencer_flags.update({j:2}) # for increasing from actual value
                        percent=((whatif_results[i][j]-float(current_dict[i][j]))/float(current_dict[i][j]))*100
                        percent_change.update({j:round(percent,2)})
                    else:
                        influencer_flags.update({j:0})
                        percent_change.update({j:0})
            else: 
                if current_dict[i]>whatif_results[i]:
                    influencer_flags.update({targetvariable:1}) # for decreasing from actual value
                    percent=((whatif_results[i]-float(current_dict[i]))/float(current_dict[i]))*100
                    percent_change.update({targetvariable:round(percent,2)})
                elif current_dict[i]<whatif_results[i]:
                    influencer_flags.update({targetvariable:2}) # for increasing from actual value
                    percent=((whatif_results[i]-float(current_dict[i]))/float(current_dict[i]))*100
                    percent_change.update({targetvariable:round(percent,2)})
                else:
                    influencer_flags.update({targetvariable:0})
                    percent_change.update({targetvariable:0})
        except ZeroDivisionError:
            if i =="Influencers":
                for j in list(current_dict["Influencers"].keys()):
                    if current_dict[i][j]>whatif_results[i][j]:
                        influencer_flags.update({j:1}) # for decreasing from actual value
                        percent_change.update({j:0})
                    elif current_dict[i][j]<whatif_results[i][j]:
                        influencer_flags.update({j:2}) # for increasing from actual value
                        percent_change.update({j:0})
                    else:
                        influencer_flags.update({j:0})
                        percent_change.update({j:0})
            else:
                if current_dict[i]>whatif_results[i]:
                    influencer_flags.update({targetvariable:1}) # for decreasing from actual value
                    percent_change.update({targetvariable:0})
                elif current_dict[i]<whatif_results[i]:
                    influencer_flags.update({targetvariable:2}) # for increasing from actual value
                    percent_change.update({targetvariable:0})
                else:
                    influencer_flags.update({targetvariable:0})
                    percent_change.update({targetvariable:0})
    whatif_results.update({"IncrementFlags": influencer_flags})
    whatif_results.update({"PercentChange":percent_change})
    output = whatif_results

    return output

#
#inputs={"TargetCertainty":71,
#            "TargetVariable":0,
#            "Influencers":{"Effort (Hrs)":4,
#                          "Team Size":0,
#                           "Schedule (Days)":8},
#           "ChangedField":"Effort (Hrs)"}
#            

            
            
#inputs={"TargetCertainty": 87,
#            "TargetVariable":12,
#            "Influencers":{"Effort":1700,
#                           "TeamSize":12,
#                           "Schedule":8},
#            "ChangedField":"TargetVariable"}           
#    
inputs={"TargetCertainty": 75,
            "TargetVariable":12,
            "Influencers":{ 
                            "charges": 1,
                            "children": 2,
                            },
            "ChangedField":"TargetVariable"}