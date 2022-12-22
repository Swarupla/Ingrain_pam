import pandas as pd
from pymongo import MongoClient
from utils import open_dbconn
import EncryptData
import base64
import json

def handle_ip_data(TemplateID,ProblemType)  :
    dbconn,dbcollection1 = open_dbconn("TemplateData")  
    data_json1 = dbcollection1.find({"TemplateID" :TemplateID}) 
    dbconn.close()
#    client = MongoClient('localhost',27017)
#    db = client.get_database(name = "inGrainDB")
#    dbcollection = db["TemplateData"]
#    data_json1 = dbcollection.find({"TemplateID" :TemplateID})    
#    client.close()
    k1 = list(data_json1)
    ip_json=k1[0]
    data_dict = ip_json.get("Features")
    tar_var = k1[0].get("TargetColumn")
    encryption = ip_json.get("isDBEncryption")
    
    if encryption==True:
        t = base64.b64decode(data_dict)     
        data_dict = eval(EncryptData.DescryptIt(t))
        
    flag_teamsize_grt_40  = None
    if ProblemType == "ADSP" or ProblemType == "RRP":
        input_selection = k1[0].get("InputSelection")
        phases_selected = []
        for i in input_selection.keys():
            if (input_selection[i]=="True" or input_selection[i]==True) and (i in list(data_dict[tar_var].keys())):
                phases_selected.append(i)
        phases_not_selected = list(set(input_selection.keys())-set(phases_selected))

        flag_check=1 # as of now manually giving value
        # here updating the total team size and total defect based on scenario
        data_dict,flag_teamsize_grt_40=update_defect_teamsize(data_dict,flag_check,phases_selected)

    SelectedCurrentRelease=None
    
    uid = ip_json.get("UniqueIdentifierName")
    if (ProblemType == "ADSP" or ProblemType =="RRP"):
        SelectedCurrentRelease = ip_json.get("SelectedCurrentRelease")
        for i in range(len(data_dict["Release Name"])):
            if data_dict["Release Name"][i] == SelectedCurrentRelease:
                index_current = i
        for i in range(len(data_dict["Release State"])):
            if i != index_current:
                data_dict["Release State"][i] = "Past"
           
        main_cols = ip_json["MainColumns"]
        main_cols_selection = ip_json["MainSelection"]
        for i in main_cols_selection.keys():
            if main_cols_selection[i] == "False" or main_cols_selection[i] == False:
                main_cols.remove(i)
        inf_cols=main_cols
        ip_dfs = {}
        for i in inf_cols:
            print(i)
            ip_dfs[i]=pd.DataFrame(data_dict[i],index = data_dict["Release State"])
            if len(phases_not_selected)>0 and i != "Schedule (Days)":
                ip_dfs.update({i: aggregate_phases(ip_dfs[i],i,phases_selected)})
            if (i=="Schedule (Days)"):
                ip_dfs.update({i: aggregate_phases(ip_dfs[i],i,phases_selected)})
                ip_dfs[i]= ip_dfs[i].drop(["Release Start Date (dd/mm/yyyy)","Release End Date (dd/mm/yyyy)"],axis=1)
    elif(ProblemType == "Generic"):
        main_cols = ip_json.get("TargetColumnList").keys()
        cols=list(set(main_cols)-set([uid]))
        ip_df = pd.DataFrame([],index=data_dict["Status"])
        for i in cols:
            ip_df[i] = data_dict[i]
        ip_dfs = ip_df
        inf_cols = list(set(cols)-set([k1[0].get("TargetColumn")]))
    return ip_dfs,inf_cols,flag_teamsize_grt_40,SelectedCurrentRelease

# updating defect and team size on the basis of density of phases data
def update_defect_teamsize(data_dict,flag_check,phases_selected):
    flag_teamsize_grt_40=0
    if flag_check==1:
        # Calculating zeros and non zeros density
#        if "Defect" in data_dict:
#            tst_defect_df=pd.DataFrame.from_dict(data_dict["Defect"], orient='columns')  
#            phse_df_defect=tst_defect_df[phases_selected]
#            percentage_zero_defect=phse_df_defect.isin([0]).sum().sum()/phse_df_defect.count().sum()*100
#            percentage_non_zeroes_defect=100-percentage_zero_defect
#            if percentage_non_zeroes_defect > 40:
#                flag_defect_grt_60=1
#                # calculate values based on phases
#                tst_defect_df["Total Defect"] = phse_df_defect.sum(axis=1)
#                # update the data_dict with updated dataframes in defect and team size
#                data_dict["Defect"]["Total Defect"] = tst_defect_df['Total Defect'].tolist()    


        if "Team Size" in data_dict.keys():
            tst_teamsize_df=pd.DataFrame.from_dict(data_dict["Team Size"], orient='columns') 
            phse_df_team=tst_teamsize_df[phases_selected]
            percentage_zero_team=phse_df_team.isin([0]).sum().sum()/phse_df_team.count().sum()*100
            percentage_non_zeroes_team=100-percentage_zero_team   
            if percentage_non_zeroes_team > 40:
                flag_teamsize_grt_40=1
                # calculate values based on phases
                tst_teamsize_df["Overall Team Size"] = phse_df_team.sum(axis=1)
                # update the data_dict with updated dataframes in defect and team size
                data_dict["Team Size"]["Overall Team Size"] = tst_teamsize_df['Overall Team Size'].tolist()    
    else:
        pass
    
    flag_teamsize_grt_40=1

    return data_dict,flag_teamsize_grt_40

def aggregate_phases(data,i,phases_selected):

    if i == "Schedule(Days)":
        overall_days =[]
        for j in range(data.shape[0]):
            overall_days.append((pd.to_datetime(data["Release End Date (dd/mm/yyyy)"][j],format="%d/%m/%Y")-pd.to_datetime(data["Release Start Date (dd/mm/yyyy)"][j],format="%d/%m/%Y")).days)
        data["Overall "+i] = overall_days
    else:
        data["Overall "+i] = data[phases_selected].sum(axis=1)
    return data
