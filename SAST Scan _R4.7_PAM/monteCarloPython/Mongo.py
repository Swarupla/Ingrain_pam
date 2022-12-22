import numpy as np
import pandas as pd
from pymongo import MongoClient
import uuid
from utils import get_dbconn
from datetime import datetime
import EncryptData  
import base64
import json

def mongo_update_ADSP(TemplateID,inf_vals,model_params,target_graph_data,results_dict_simulation,uncertainty_dict,tar_var,inf_cols,flag_teamsize_grt_40,SelectedCurrentRelease):
#    client = MongoClient('localhost',27017)
#    db = client.get_database(name = "inGrainDB")
    db,dbconn = get_dbconn()
    dbcollection = db["TemplateData"] 
    data_json = dbcollection.find({"TemplateID" :TemplateID})
    k=list(data_json)
    sim_db = db["SimulationResults"]
    SimulationId = str(uuid.uuid4())
    ProblemType =k[0].get("ProblemType")
    version = k[0].get("Version")
    create_details1 = k[0].get("CreatedOn")
    create_details2 = k[0].get("CreatedByUser")
    TargetColumn = k[0].get("TargetColumn")
    mod_detail1 = k[0].get("ModifiedOn")
    mod_detail2 = k[0].get("ModifiedByUser")
    client_uid = k[0].get("ClientUID")
    dcuid = k[0].get("DeliveryConstructUID")
    usecaseid = k[0].get("UseCaseID")
    flags,percentchange = {},{}
    ################Changes for Flags##############
    for i in inf_cols:
        flags.update({i[8:]:0})
        percentchange.update({i[8:]:0})
    ###############################################
    if (int(uncertainty_dict["TargetCertainty"]) >= 85):
        Observation = "With the "+ str(int(round(target_graph_data[3][0])))+" defects closed, the certainty of completing the release is "+str(int(uncertainty_dict["TargetCertainty"]))+ "%. There is "+str(100-int(uncertainty_dict["TargetCertainty"]))+"% risk with respect to delivered defects. Monitor the critical subprocesses identified through Sensitivity Analysis."
    else:
        Observation = "With the "+ str(int(round(target_graph_data[3][0])))+" defects closed, the certainty of completing the release is "+str(int(uncertainty_dict["TargetCertainty"]))+ "%. There is "+str(100-int(uncertainty_dict["TargetCertainty"]))+"% risk with respect to delivered defects. Perform Scenario Analysis to mitigate the risk identified as the certainty is <85%."

    encryption = k[0].get("isDBEncryption")
    sim_results = {
            "TargetCertainty":int(round(uncertainty_dict["TargetCertainty"])),
            "TargetVariable": int(round(target_graph_data[3][0])),
            "Influencers":inf_vals,
            "CurrentInfluencers":inf_vals,
            "InfluencerDistributions":uncertainty_dict["InfluencerDistributions"],
            "Observation":Observation,
            "ModelParams":model_params,
            "Target_Distribution":{"mean":target_graph_data[0]["mean"],"standard_deviation":target_graph_data[0]["standard_deviation"]}}
    if encryption==True:
            for i in sim_results:
                if i=="Observation":
                    sim_results[i] = EncryptData.EncryptIt((sim_results[i]))
                else:
                    sim_results[i] = EncryptData.EncryptIt(json.dumps(sim_results[i]))     
            percentchange = EncryptData.EncryptIt(json.dumps(percentchange))
            flags = EncryptData.EncryptIt(json.dumps(flags))
    sim_db.insert_one({
            "SelectedCurrentRelease":SelectedCurrentRelease,
            "IncrementFlags": flags,
            "PercentChange":percentchange,
            "TemplateVersion": version,
            "TemplateID" : TemplateID,
            "SimulationVersion":"Simulation Version 1",
            "isDBEncryption": encryption,
            "SimulationID": SimulationId,
            "ClientUID":client_uid,
            "DeliveryConstructUID" : dcuid,
            "UseCaseID":usecaseid,
            "ProblemType":ProblemType,
            "TargetColumn":TargetColumn,
            "CreatedOn" : create_details1,
            "CreatedByUser":create_details2,
            "ModifiedOn" : mod_detail1,
            "ModifiedByUser":mod_detail2
            })
    
    db.SimulationData.insert_one({
            "TemplateID" : TemplateID,
            "ClientUID":client_uid,
            "DeliveryConstructUID" : dcuid,
            "ProblemType":ProblemType,
            "isDBEncryption": encryption,
            "UseCaseID":usecaseid,
            "TargetVariable":tar_var[8:],
            "Influencers" : inf_cols,
            "CreatedOn" : create_details1,
            "CreatedByUser":create_details2,
            "ModifiedOn" : mod_detail1,
            "ModifiedByUser":mod_detail2,
            "FlagTeamSize":flag_teamsize_grt_40
            })
    
    data_json = db.SimulationResults.find({"TemplateID" :TemplateID})
    k=list(data_json)
    Id = k[0].get("_id")
    db.SimulationResults.update_one({"_id" : Id},{ "$set": sim_results })
    
    data_json = db.SimulationData.find({"TemplateID" :TemplateID})
    k=list(data_json)
    Id = k[0].get("_id")
    db.SimulationData.update({"_id" : Id},{ "$set": results_dict_simulation })
    dbconn.close()
#    client.close()
    return None

def mongo_update_generic(TemplateID,inf_vals,model_params,target_graph_data,results_dict_simulation,uncertainty_dict,top_inf,default_cols,tar_var,inf_cols):

    #client = MongoClient('localhost',27017)
    #db = client.get_default_database(default = "inGrainDB")
    db,dbconn = get_dbconn()
    dbcollection = db["TemplateData"] 
    data_json = dbcollection.find({"TemplateID" :TemplateID})
    k=list(data_json)
    ProblemType =k[0].get("ProblemType")

    sim_db = db["SimulationResults"]
    SimulationId = str(uuid.uuid4())
    version = k[0].get("Version")
    create_details1 = k[0].get("CreatedOn")
    create_details2 = k[0].get("CreatedByUser")
    mod_detail1 = k[0].get("ModifiedOn")
    mod_detail2 = k[0].get("ModifiedByUser")
    client_uid = k[0].get("ClientUID")
    TargetColumn = k[0].get("TargetColumn")
    dcuid = k[0].get("DeliveryConstructUID")
    usecaseid = k[0].get("UseCaseID")
    encryption = k[0].get("isDBEncryption")
    
    flags,percentchange = {},{}
    for i in inf_cols:
        flags.update({i:0})
        percentchange.update({i:0})
    
    sim_results ={
            "TargetCertainty":float(round(uncertainty_dict["TargetCertainty"],2)),
            "TargetVariable": float(round(target_graph_data[3][0],2)),
            "Influencers":inf_vals,
            "CurrentInfluencers":inf_vals,
            "TopInfluencers":top_inf,
            "DefaultColumns":default_cols,
            "ModelParams":model_params,
            "Target_Distribution":{"mean":float(target_graph_data[0]["mean"]),"standard_deviation":float(target_graph_data[0]["standard_deviation"])}
            }
    if encryption==True:
            for i in sim_results:
                sim_results[i] = EncryptData.EncryptIt(json.dumps(sim_results[i]))    
            percentchange = EncryptData.EncryptIt(json.dumps(percentchange))
            flags = EncryptData.EncryptIt(json.dumps(flags))
    sim_db.insert_one({
            "IncrementFlags": flags,
            "PercentChange":percentchange,
            "TemplateVersion": version,
            "TemplateID" : TemplateID,
            "isDBEncryption": encryption,
            "SimulationVersion":"Simulation Version 1",
            "SimulationID": SimulationId,
            "ClientUID":client_uid,
            "DeliveryConstructUID" : dcuid,
            "UseCaseID":usecaseid,
            "ProblemType":ProblemType,
            "TargetColumn":TargetColumn,
            "CreatedOn" : str(create_details1),
            "CreatedByUser":str(create_details2),
            "ModifiedOn" : str(mod_detail1),
            "ModifiedByUser":str(mod_detail2)
            })
    
    db.SimulationData.insert_one({
            
            "TemplateID" : TemplateID,
            "ClientUID":client_uid,
            "isDBEncryption": encryption,
            "DeliveryConstructUID" : dcuid,
            "ProblemType":ProblemType,
            "UseCaseID":usecaseid,
            "TargetVariable":tar_var,
            "Influencers":top_inf,
            "CreatedOn" : create_details1,
            "CreatedByUser":create_details2,
            "ModifiedOn" : mod_detail1,
            "ModifiedByUser":mod_detail2
            })
    
    data_json = db.SimulationResults.find({"TemplateID" :TemplateID})
    k=list(data_json)
    Id = k[0].get("_id")
    db.SimulationResults.update_one({"_id" : Id},{ "$set": sim_results })
    
    data_json = db.SimulationData.find({"TemplateID" :TemplateID})
    k=list(data_json)
    Id = k[0].get("_id")
    db.SimulationData.update_one({"_id" : Id},{ "$set": results_dict_simulation })
    dbconn.close()
#    client.close()
    return None

def update_mongo_pylogs(TemplateID,process,e,SimulationID=""):
    db,dbconn =get_dbconn()
    d = datetime.now()
    db.pylogs.insert_one({
            "TemplateID":TemplateID,
            "SimulationID":SimulationID,
            "Process":process,
            "ExceptionOccuredAt":d,
            "Error": e})
    dbconn.close()
    return None