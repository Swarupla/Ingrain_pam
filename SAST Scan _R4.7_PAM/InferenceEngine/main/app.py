# -*- coding: utf-8 -*-
"""
Created on Fri Apr  2 10:56:22 2021

@author: maddikuntla.p.kumar

"""

import platform

if platform.system() == 'Linux':
    conf_path = '/pythonconfig.ini'
    conf_path_pheonix = '/pheonixentityconfig.ini'
    work_dir = ''
elif platform.system() == 'Windows':
    conf_path = '\pythonconfig.ini'
    conf_path_pheonix = '\pheonixentityconfig.ini'
    work_dir = ''
    from requests_negotiate_sspi import HttpNegotiateAuth

import configparser, os
mainPath = os.getcwd() + work_dir
import sys

sys.path.insert(0, mainPath)
from datetime import datetime

import pandas as pd
import numpy as np
#from pandas import *
import numpy as np
# from libraries.settings import *
from scipy.stats.stats import pearsonr
import itertools
import statsmodels.api as sm 
from statsmodels.formula.api import ols
import xlrd
from scipy import stats 
import json
import dateutil.parser
from SSAIutils import utils
from SSAIutils.inference_engine_utils import *
#from MongoDBConnection import *
from SSAIutils import EncryptData
from  main.inferenceEngineSource import *

correlationId = sys.argv[1]
requestId = sys.argv[2]
InferenceConfigId = sys.argv[3]
pageInfo = sys.argv[4]
userId = sys.argv[5]

"""
correlationId = 'outliercheck'#'ticketdata'#'metrics_data'
requestId = 'bd153ee9-ba92-44c1-bbb7-3593563818d2'
InferenceConfigId = 'InferenceConfigId'
pageInfo = 'NA'
userId = 'praveen kumar'

correlationId =  'hessdata_updated' #'04062021-ioincidentdata'
requestId = 'bd153ee9-ba92-44c1-bbb7-3593563818d2'
InferenceConfigId = 'NA'
pageInfo = 'GenerateInflow'
userId = 'praveen kumar'


"""
logger = utils.logger('Get', correlationId)
EncryptionFlag = utils.getEncryptionFlag(correlationId)

try: 
    
    if pageInfo == 'GenerateVolumetric':
            generateVolumetricPageInfo(correlationId,requestId,InferenceConfigId,pageInfo,userId,EncryptionFlag,logger)
                  
    if pageInfo == 'GetFeatures':
            getFeaturesPageInfo(correlationId,requestId,InferenceConfigId,pageInfo,userId,EncryptionFlag,logger)
            
    if pageInfo == 'GenerateNarratives':
            generateNarrativesPageInfo(correlationId,requestId,InferenceConfigId,pageInfo,userId,EncryptionFlag,logger)
            
                        
            
        
except Exception as e:
    utils.updQdb(correlationId, 'E', str(e.args[0]), pageInfo, userId,modelName=None, problemType=None, UniId=None,retrycount=3,Incremental=False,requestId=requestId)
    utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
    #utils.logger(logger, correlationId, 'ERROR', str(e.args[0]))
