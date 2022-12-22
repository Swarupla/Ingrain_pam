# -*- coding: utf-8 -*-
"""
Created on Tue Feb  4 08:06:15 2020

@author: saurav.b.mondal
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
def approx_num_bins(num_unique_arr,pred_arr):
    import numpy as np
    approx_dict = {}
    approx_val=[]
    count=0
    for i in pred_arr:        
        dist=np.float("inf")
        count= count+1
        if str(i)=="nan":
                approx_val.append(0)
        else:
            for j in num_unique_arr:
                temp = abs(float(j)-float(i))
                approx_dict.update({temp : j})
                if temp <= dist:
                    dist = temp
            approx_val.append(approx_dict[dist])
    return approx_val
            
#a=[1,2,3,4,6]
#
#b=[2.3,4.6,3.8,5.8,"nan"]
#len(approx_num_bins(a,b))