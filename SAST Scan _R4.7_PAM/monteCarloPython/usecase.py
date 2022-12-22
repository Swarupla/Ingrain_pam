import pandas as pd
import numpy as np
from scipy.optimize import linprog
from scipy.stats import norm

def usecase_one(input_dict,model_params,target_distribution,ProblemType):
    """
    Input: 
        input_dict - Dictionary containing influencer, target values(None), and uncertainty(None).
        model_params- model coefficients and intercept.
    Output:
        whatif_dict - Dictionary containing influencer, target values, and uncertainty.
    """
    target  = np.dot(list(input_dict["Influencers"].values()),list(model_params[1]))
    target += model_params[0]
    if(target < 0):
        target = 0
    uncertainty = round((norm.cdf(target,target_distribution["mean"],target_distribution["standard_deviation"]))*100,2)
    if (ProblemType == "ADSP" or ProblemType == "RRP"):
        input_dict.update({"TargetCertainty":round(uncertainty,2),"TargetVariable":int(round(target))})
    else:
        input_dict.update({"TargetCertainty":round(uncertainty,2),"TargetVariable":round(target,2)})
        
    whatif_dict = input_dict
    return whatif_dict

def usecase_two(input_dict,model_params,inf_cols,target_distribution,ProblemType):
    """
    Input: 
        input_dict - Dictionary containing influencer, target values(None), and uncertainty.
        model_params- model coefficients and intercept.
    Output:
        whatif_dict - Dictionary containing influencer, target values, and uncertainty.
    """
    params = []
    int_list = []
    params.append(model_params[0])
    int_list.append(1)
    B=[[1,1]]
    percent_change = 0.15
    for i in model_params[1]:
        params.append(i)
        int_list.append(0)
#    infs_cols = list(input_dict["Influencers"].keys())
#    for i in infs_cols:
#        if (("Total "+i)=="Schedule (Days)"):
#            percent_change = 0.15
    infs = list(input_dict["Influencers"].values())
    for i in infs:
        if [(i-(percent_change*i))>0]:
            B.append([(i-(percent_change*i)),(i+(percent_change*i))] )
        else:
            B.append([1,(i+(percent_change*i))] )
    uncertainty = int(input_dict["TargetCertainty"])
    if uncertainty==100:
        uncertainty = 99.99
    elif uncertainty==0:
        uncertainty=0.01
    target = round(norm.ppf(uncertainty/100,target_distribution["mean"],target_distribution["standard_deviation"]),2)
    # Equality equations, LHS
    if (target<0):
        target = 0
    elif (str(target) == "nan"):
        target = input_dict["TargetVariable"]
    A_eq = [params,int_list]
    # Equality equations, RHS
    B_eq = [target,1]
    cost = params
    es_bounds = linprog(cost, A_eq=A_eq, b_eq=B_eq, bounds=tuple(B), method='revised simplex')
    influencer_values = es_bounds["x"][1:]
    if ProblemType=="ADSP" or ProblemType == "RRP":
        influencer_values = [round(i) for i in influencer_values]
        input_dict.update({"TargetVariable":int(round(target))})
    else:
        influencer_values = [round(i,2) for i in influencer_values]  
        input_dict.update({"TargetVariable":round(target,2)})
    input_dict.update({"Influencers" : dict(zip(inf_cols,influencer_values))})
    whatif_dict = input_dict
    return whatif_dict

def usecase_three(input_dict,model_params,inf_cols,target_distribution,ProblemType):
    """
    Input: 
        input_dict - Dictionary containing influencer, target values, and uncertainty(None).
        model_params- model coefficients and intercept.
    Output:
        whatif_dict - Dictionary containing influencer, target values, and uncertainty.
    """
    params = []
    int_list = []
    params.append(model_params[0])
    int_list.append(1)
    B=[[1,1]]
    percent_change = 0.15
    for i in model_params[1]:
        params.append(i)
        int_list.append(0)
#    infs_cols = list(input_dict["Influencers"].keys())
#    for i in infs_cols:
#        if (("Total "+i)=="Schedule (Days)"):
#            percent_change = 0.15
    infs = list(input_dict["Influencers"].values())
    for i in infs:
        if [(i-(percent_change*i))>0]:
            B.append([(i-(percent_change*i)),(i+(percent_change*i))] )
        else:
            B.append([1,(i+(percent_change*i))] )
    target = input_dict["TargetVariable"]
    if (target<0):
        target=0
    uncertainty = round(norm.cdf(int(input_dict["TargetVariable"]),target_distribution["mean"],target_distribution["standard_deviation"])*100,2)
    # Equality equations, LHS
    A_eq = [params,int_list]
    # Equality equations, RHS
    B_eq = [target,1]
    cost = params
    es_bounds = linprog(cost, A_eq=A_eq, b_eq=B_eq, bounds=tuple(B), method='revised simplex')
    influencer_values = es_bounds["x"][1:]

    if ProblemType=="ADSP" or ProblemType == "RRP":
        influencer_values = [(round(i)) for i in influencer_values]
        input_dict.update({"TargetVariable":int(round(target))})
    else:
        influencer_values = [round(i,2) for i in influencer_values]  
        input_dict.update({"TargetVariable":round(target,2)})

    input_dict.update({"Influencers" : dict(zip(inf_cols,influencer_values))})
    input_dict.update({"TargetCertainty":uncertainty})
    whatif_dict = input_dict
    return whatif_dict

#input_dict={"Uncertainty": None,
#            "Target":12,
#            "Influencers":{'PublishedRelativePerformance': 106.0,
#                             'MaxMainMemory(kB)': 11834.0,
#                             'MinMainMemory(kB)': 2877.0,
#                             'MaxChannels': 18.0,
#                             }}