# -*- coding: utf-8 -*-
"""
Created on Thu May  9 12:44:45 2019

@author: sravan.kumar.tallozu
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd

def main(data_cols,datasize,datappcols,datappsize,model,lencm,ertmodel):
     
    model_enc = lencm.transform([model])    
    data = pd.DataFrame([{'datac':data_cols,'datasize':datasize,'Pdatac':datappcols,'pdatasize':datappsize,'model_L':model_enc[0]}])
    
    ERT = ertmodel.predict(data)
    
    return ERT