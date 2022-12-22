import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
import numpy as np
import itertools

def sp_filter(data,tar_var):
    """
    ***Success_Percentage_Filter***
    Inputs : 
        data - Dataframe(containing influencers and target) predicting the desired target value.
    Output :
        success_cartesian_df - Filtered Dataframe(picking classes from each influencer which resulted in desired target the most.)
    """
    inf_data = data.drop(tar_var,axis=1)
    success_class_dict = {}    
    
    for i in list(inf_data.columns):
        if(np.size(list(inf_data[i].unique())) > 2):
            n_classes = 2
            count_df = data.groupby(i).count()
            top_counts = np.sort(count_df[tar_var])[-n_classes:]
            top_classes = np.array(count_df[count_df[tar_var]==top_counts[0]].index[0])
            top_classes = np.append(top_classes,count_df[count_df[tar_var]==top_counts[1]].index[0])
            
        else:
            top_classes = np.array([])
            n_classes = 1
            count_df = data.groupby(i).count()
            top_counts = np.sort(count_df[tar_var])[-n_classes:]
            top_classes = np.append(top_classes,count_df[count_df[tar_var]==top_counts[0]].index[0])
        
        success_class_dict[i] = list(top_classes)
    
    success_cartesian_df=pd.DataFrame([],columns=list(inf_data.columns))
    inf_classes = [success_class_dict[i] for i in list(inf_data.columns)]
    
    for i in itertools.product(*inf_classes): 
        success_cartesian_df = success_cartesian_df.append(pd.DataFrame([list(i)],columns=inf_data.columns),ignore_index=True)
   
    return success_cartesian_df