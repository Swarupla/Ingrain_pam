# -*- coding: utf-8 -*-
"""
Created on Fri Jan 25 10:40:24 2019

@author: sravan.kumar.tallozu
"""

import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import six
import sys
sys.modules['sklearn.externals.six'] = six
from imblearn.over_sampling import SMOTE
from imblearn.combine import SMOTEENN


def smote(X,Y):
    smt = SMOTE(random_state=50,sampling_strategy='minority')   
    x_train, y_train = smt.fit_resample(X, Y)
    return x_train, y_train


def smoteENN(X,Y):
    smt = SMOTEENN(random_state=50)
    x_train, y_train = smt.fit_resample(X, Y)
    return x_train, y_train      

def check_SMOTE_multiclass(y_data,ratio=None,user_consent=None):
    if user_consent=="True":
        if ratio==None:
            ratio=0.2
        target_value_count_dict=y_data.value_counts().to_dict()
       
        new={}
   
        for k,v in target_value_count_dict.items():
            new[k]=round(v/len(y_data),2)
   
        max_val=max(list(new.values()))
        for k,v in new.items():  
            new[k]=round(v/max_val,2)
       
        ratio_less_than=[k for (k,v) in new.items() if v < ratio]
       
        if len(ratio_less_than)>0:
            return "True"
        if len(ratio_less_than)==0:
            return "False"
    elif user_consent==False:
        return "False"
    else:
        return "False"