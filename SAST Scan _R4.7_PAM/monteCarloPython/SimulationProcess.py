import numpy as np
import pandas as pd
from ip_data import handle_ip_data,aggregate_phases
from utils import prepare_influencer_graph_data,prepare_target_graph_data,random_sample,model_data,feature_selection,open_dbconn
from Sensitivity import random_sample_subprocess,calculate_sensitivity,sensitivity_report,sensitivity_report_generic
from pymongo import MongoClient
from Mongo import mongo_update_ADSP,mongo_update_generic
import configparser,os
import EncryptData  
import base64
import json
import file_encryptor
import utils
from datetime import datetime

#TemplateID="04fb8405-49b8-49e1-8ff7-d2ca6bf0f915"

config = configparser.RawConfigParser()
#configpath = "/var/www/monteCarloPython/pythonconfig.ini"
conf_path = "/pythonconfig.ini"
configpath = str(os.getcwd()) + str(conf_path)

try:
    config.read(configpath)
except UnicodeDecodeError:
    print("Decrypting Config File : SimulationProcess.py")
    config = file_encryptor.get_configparser_obj(configpath)


def Simulation(TemplateID):
#    client = MongoClient('localhost',27017)
#    db = client.get_database(name = "inGrainDB")
#    dbcollection = db["TemplateData"]
#    data_json = dbcollection.find({"TemplateID" :TemplateID})    
#    client.close()
    logger = utils.logger('Get', TemplateID)
    utils.logger(logger, TemplateID, 'INFO', ('Runsimulation started successfully'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    dbconn,dbcollection = open_dbconn("TemplateData")
    data_json = dbcollection.find({"TemplateID" :TemplateID})    
    dbconn.close()
    k = list(data_json)
    ProblemType=k[0].get("ProblemType")
    encryption = k[0].get("isDBEncryption")
    if(ProblemType=="ADSP" or ProblemType == "RRP"):
        ip_data,_,flag_teamsize_grt_40,SelectedCurrentRelease = handle_ip_data(TemplateID,ProblemType)
        tar_var = k[0].get("TargetColumn")
        main_cols =k[0].get("MainColumns")
        
        main_cols_selection = k[0].get("MainSelection")
        for i in main_cols_selection.keys():
            if main_cols_selection[i] == "False" or main_cols_selection[i] == False:
                main_cols.remove(i)
        
        input_selection = k[0].get("InputSelection")
        phases_selected = []
        for i in input_selection.keys():
            if input_selection[i]=="True" or input_selection[i]==True and (i in list(ip_data[tar_var].columns)) :
                phases_selected.append(i)
        phases_not_selected = list(set(input_selection.keys())-set(phases_selected))         
                
        for i in main_cols:
            if phases_not_selected in main_cols:
                ip_data[i].drop(phases_not_selected,axis=1,inplace=True) 
        
        sim_df = pd.DataFrame([])
        for i in main_cols:
            sim_df["Overall "+i] = ip_data[i]["Overall "+i]
        ongoing_df = sim_df.loc[["Current"]].drop("Overall "+tar_var,axis=1)
        sim_df = sim_df.drop("Current")
        temp_data = sim_df.copy()
        temp_data = temp_data.drop("Overall "+tar_var,axis=1)
        inf_cols = list(temp_data.columns)
        tar_var = "Overall "+tar_var
        sim_df= sim_df.astype(int)
        
        model_params = model_data(sim_df,tar_var,inf_cols)

        temp_cols = inf_cols.copy()
        
        sampled_df = random_sample(sim_df,tar_var,ongoing_df)
        
        target_graph_data =  prepare_target_graph_data(sampled_df,model_params,tar_var,ongoing_df,ProblemType)
        
        influencer_graph_data = prepare_influencer_graph_data(sampled_df,tar_var,ongoing_df,inf_cols)
        
        subprocess_dfs = random_sample_subprocess(ip_data,inf_cols)
        sens_value = {}
        for i in subprocess_dfs:
            d = subprocess_dfs[i]
            sub_tar_var = list(d.columns)[-1]
            sens_value.update({i : calculate_sensitivity(d,sub_tar_var)})
        sens_report = sensitivity_report(sampled_df,inf_cols,tar_var)
        sens_report[tar_var[8:]][0] = round(target_graph_data[3][0],2)
        
        #Sensitivity for overall column w.r.t to defect
        main_sens_val = calculate_sensitivity(sim_df,tar_var)
        temp_main_cols = []
        for i in list(main_sens_val.keys()):
            temp_main_cols.append(i[8:])
        main_sens_val = dict(zip(temp_main_cols,list(main_sens_val.values())))
        temp_main = main_cols.copy()
        temp_main.remove("Defect")
        temp_main_sens_val = {}
        for i in temp_main:
            temp_main_sens_val.update({i:main_sens_val[i]})
        target_graph_data[0].update({"Sensitivity_Analysis":temp_main_sens_val})

        results_dict_simulation = {}
        cols = []
        for i in list(ongoing_df.columns):
            cols.append(i[8:])                    # Removing Total from column names in ongoing_df
        ong_vals = ongoing_df.values[0]
        ong_vals = [int(i) for i in ong_vals]     # from line 81 to 86 we are transposing ongoing_df
        inf_vals = dict(zip(cols,ong_vals))        
        uncertainty_dict = {"Influencers":{},"InfluencerDistributions":{}}        
        inf_cols.append(tar_var)
        for i in inf_cols:
            if (i == tar_var):
                i = i[8:]
                results_dict_simulation.update({i : target_graph_data[0]})
                uncertainty_dict.update({"TargetCertainty":target_graph_data[2]})
            else:
                temp =  {"histogram" : influencer_graph_data[0][i]} 
                sub_tar_var =i
                i = i[8:]
                temp.update({"Sensitivity_Analysis" : sens_value[i]})
                results_dict_simulation.update({i : temp})
                uncertainty_dict["Influencers"].update({i : influencer_graph_data[2]["Overall "+i+" certainty"]})
                uncertainty_dict["InfluencerDistributions"].update({i:influencer_graph_data[3][sub_tar_var]})
        results_dict_simulation.update({"SensitivityReport":sens_report})
               
        if encryption==True:
            for i in results_dict_simulation:
                results_dict_simulation.update({i:EncryptData.EncryptIt(json.dumps(results_dict_simulation[i]))})

        model_params =[model_params[0], dict(zip(temp_cols,list(model_params[1])))]
        
        mongo_update_ADSP(TemplateID,inf_vals,model_params,target_graph_data,results_dict_simulation,uncertainty_dict,tar_var,inf_cols,flag_teamsize_grt_40,SelectedCurrentRelease)

#################################################################################################
             
    elif(ProblemType=="Generic"):
        
        ip_data,inf_cols,_,_ = handle_ip_data(TemplateID,ProblemType)
        tar_var = k[0].get("TargetColumn")    
        temp = ip_data[tar_var]
        sim_df = ip_data[inf_cols]
        sim_df[tar_var] =list(ip_data[tar_var])
        ongoing_df = sim_df.loc[["Current"]].drop(tar_var,axis=1)
        sim_df = sim_df.drop("Current")
        top_inf = []
        default_cols = []
        
        if (len(inf_cols)>4):
            inf,top_inf= feature_selection(sim_df,inf_cols,tar_var)#check for nonzero coefficients
		
            random_df = sim_df[inf]
            ongoing_df = ongoing_df[inf]
            random_df[tar_var] = list(sim_df[tar_var])
            sim_df = random_df
            inf_cols = inf
            ongoing_df = ongoing_df[inf]
            for i in inf_cols:
                if i not in top_inf:
                    default_cols.append(i)
            default_cols = sim_df[default_cols].mean().round().to_dict()
        else:
            top_inf = inf_cols
        model_params = model_data(sim_df,tar_var,inf_cols)
        
        sampled_df = random_sample(sim_df,tar_var,ongoing_df)
        
        target_graph_data =  prepare_target_graph_data(sampled_df,model_params,tar_var,ongoing_df,ProblemType)
        
        sens_df = sampled_df[top_inf]
        sens_df[tar_var]=list(sampled_df[tar_var])
        sens_value = calculate_sensitivity(sens_df,tar_var)
        target_graph_data[0].update({"SensitivityAnalysis":sens_value})
        results_dict_simulation,uncertainty_dict={},{}
        

        
        results_dict_simulation.update({tar_var : target_graph_data[0]})
        uncertainty_dict.update({"TargetCertainty":target_graph_data[2]})
        uncertainty_dict.update({"TargetVariable":target_graph_data[3][0]})
        vals = []
        if (len(top_inf)>0):
            ongoing_df = ongoing_df[top_inf]
        for i in list(ongoing_df.values[0]):
            vals.append(str(i))
        ong_vals = ongoing_df.values[0]
        ong_vals = [float(round(i,2)) for i in ong_vals]
        inf_vals = dict(zip(list(ongoing_df.columns),ong_vals))
        sens_report = sensitivity_report_generic(sens_df,top_inf,tar_var)
        sens_report[tar_var][0] = round(target_graph_data[3][0],2)
        results_dict_simulation.update({"SensitivityReport":sens_report})

        if encryption==True:
            for i in results_dict_simulation:
                results_dict_simulation.update({i:EncryptData.EncryptIt(json.dumps(results_dict_simulation[i]))})
        inf_cols.append(tar_var)
        model_params =[model_params[0], dict(zip(inf_cols,list(model_params[1])))]
        mongo_update_generic(TemplateID,inf_vals,model_params,target_graph_data,results_dict_simulation,uncertainty_dict,top_inf,default_cols,tar_var,inf_cols)
        
    return "Success"