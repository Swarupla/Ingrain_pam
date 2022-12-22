# -*- coding: utf-8 -*-
"""
Created on Wed May 22 07:28:04 2019
    
@author: nitin.john.james
"""

import pandas as pd
import numpy as np
from pandas.api.types import CategoricalDtype
import configparser
from SSAIutils import utils
from flask import jsonify

class VisualizationData():
    def __init__(self):
        self.inputdata = None
        self.inputdatatypes=None
        self.data=None
        self.config = configparser.ConfigParser()
        self.config.read('D:/SSAIWeb/chartconfig.ini')
        self.availableCharts = self.config.sections()


    def postData(self,correlationId):        
        #self.inputdata = utils.get_DataCleanUP_FilteredData_visualization(correlationId)
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            self.inputdata = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            self.inputdata = utils.data_from_chunks(corid=correlationId,collection="PS_IngestedData") 
        inputdataColumns = self.inputdata.columns
        self.inputdatatypes = ["categoric" if isinstance(each,CategoricalDtype) else "numeric" if isinstance(each,np.dtype) else each for each in self.inputdata.dtypes.tolist()]
        
        axis = inputdataColumns.tolist()
        self.inputdatatypes = dict(zip(axis,self.inputdatatypes))
        
        self.data = {"axis":axis,"charts":self.availableCharts}
        for chart in self.availableCharts:
            self.data[chart] = []
            for key in self.config[chart]:
                self.data[chart].append(key)
        
        return jsonify(self.data)

    def renderChart(self,params):
        print (params)
        charttype = params["charttype"]
        chartselection = {}
        axis = []
        for chartaxis in self.data[charttype]:
            chartselection[chartaxis] = self.inputdatatypes[params[chartaxis]]
            axis.append(params[chartaxis])
            if (self.inputdatatypes[params[chartaxis]] not in self.config[charttype][chartaxis]):
                return str(chartaxis)+" datatype not compatable with this chart"
        
        selectedDf = self.inputdata[axis]
        selectedDf.replace(np.nan, 0, regex=True,inplace=True)
  
        return jsonify(data = selectedDf.to_dict(orient="records"), dtype = chartselection)
        

