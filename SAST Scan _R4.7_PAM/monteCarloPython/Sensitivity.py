import pandas as pd
import numpy as np
import statistics as st
from scipy import stats
import configparser, os
import file_encryptor

config = configparser.RawConfigParser()
conf_path = "/pythonconfig.ini"
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    print("Decrypting Config File : Sensitivity.py")
    config = file_encryptor.get_configparser_obj(configpath)

def random_sample_subprocess(ip_dfs,inf_cols,family="normal"):
    """
    
    Only for ADSP
    
    """
    np.random.seed(1)
    ip_sampled_dfs = {}
    for j in inf_cols:
        sums = []
        tar_var = j
        j=j[8:]
        ip_dfs_copy = ip_dfs[j].copy()
        ip_dfs_copy = ip_dfs_copy.drop(labels=tar_var,axis=1)  
        sample_df = pd.DataFrame([])
        cols = list(ip_dfs_copy.columns)
        for i in cols:
            col_arr  = np.array(ip_dfs[j][i])
            if (family == "normal" ):
                col_mean = st.mean(col_arr)
                col_sd   = st.stdev(col_arr)
                sample_df[i] = np.random.normal(col_mean,col_sd,int(config["constants"]["iterations"]))#invgaussian
            else:
                col_arr = np.log(col_arr)
                col_mean = st.mean(col_arr)
                col_sd   = st.stdev(col_arr)
                sample_df[i] = np.random.lognormal(col_mean,col_sd,int(config["constants"]["iterations"]))
        for i,row in enumerate(sample_df.iterrows()):
            inf_df = pd.DataFrame([dict(row[1])])
            sums.append(np.sum(inf_df.values[0]))
        sample_df[tar_var] = sums
        ip_sampled_dfs.update({j : sample_df})
    return ip_sampled_dfs

def calculate_sensitivity(df,tar_var):
    """
    Input:
        df - A dataframe containing the required features to calculate sensitivity
        tar_var - The target variable
    Output:
        sensitivity_dict - Contains sensitivity value for all the influencers w.r.t target variable
    """
    sensitivity_dict = {}
    
    inf_cols = list(set(list(df.columns))-set([tar_var]))

    for i in inf_cols:
        
        x=df[i]
        
        y = df[tar_var]
        
        slope, intercept, r_value, p_value, std_err = stats.linregress(x,y)
        
        r2 = r_value**2
        
        sensitivity_dict.update({i : r2*100 })
    
    sensitivity_dict = sorted(sensitivity_dict.items(), key=lambda x: x[1], reverse=True)
    
    d = {}
    for i in sensitivity_dict:
        d.update({i[0]:i[1]})
    sensitivity_dict = d
    return  sensitivity_dict


def sensitivity_report(df,inf_cols,tar_var):
    #df = df.drop(tar_var,axis=1)
    tmp = pd.DataFrame([],index=list(df.columns))
    tmp["SimulatedRecord"] = list(round(df).iloc[-1])
    tmp["Mean"] = list(round(df.mean(),2))
    tmp["StandardDeviation"] = list(round(df.std(),2))
    d = {}
    for j in list(df.columns):
        d.update({j[8:] : list(tmp.loc[j])})
    return d

def sensitivity_report_generic(df,inf_cols,tar_var):
    #df = df.drop(tar_var,axis=1)
    tmp = pd.DataFrame([],index=list(df.columns))
    tmp["SimulatedRecord"] = list(round(df).iloc[-1])
    tmp["Mean"] = list(round(df.mean(),2))
    tmp["StandardDeviation"] = list(round(df.std(),2))
    d = {}
    for j in list(df.columns):
        d.update({j : list(tmp.loc[j])})
    return d