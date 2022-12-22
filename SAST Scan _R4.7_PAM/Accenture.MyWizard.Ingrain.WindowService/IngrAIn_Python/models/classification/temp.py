# -*- coding: utf-8 -*-
"""
Created on Thu Aug 29 05:01:48 2019

@author: harsh.nandedkar
"""
import logging
from sklearn.linear_model import LogisticRegression as LogReg
import warnings
warnings.filterwarnings('ignore')
from SSAIutils import utils
from flask import jsonify
from datapreprocessing import Train_Cross_Validate
from evaluation import Eli5_M
import time
from evaluation import M_Evaluation
from evaluation import MultiClass_Evaluation
import numpy as np
from datapreprocessing import DataEncoding

dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
data_json = dbcollection.find({"CorrelationId" :correlationId})    
dbconn.close()
target_variable = data_json[0].get('TargetColumn')  
        
dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
data_json = dbprocollection.find({"CorrelationId" :correlationId}) 
dbproconn.close()
Data_to_Encode=data_json[0].get('DataEncoding')    
LEcols = []
encoders = {}
        
for keys,values in Data_to_Encode.items():                
    if values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') == 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
        LEcols.append(keys)
    if target_variable in LEcols:
        lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
        list1=[]
        for col_LE in Lenc_cols:
            list1.append(col_LE[:-2])

if LEcols:            
            Y_test,data_le = DataEncoding.Label_Encode_Dec(lencm,data,LEcols)
            encoders.update({'LE':{lencm:Lenc_cols}})