import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
warnings.simplefilter(action='ignore', category=FutureWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from statsmodels.tsa.holtwinters import ExponentialSmoothing
from math import sqrt
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_squared_error
from SSAIutils import utils
import time
from multiprocessing import cpu_count
from datetime import datetime
from pandas import Timestamp
ncpu = cpu_count()
from evaluation import regression_evaluation
from joblib import Parallel, delayed

import platform
def exponentialSmoothing(train, duration, cfg, seasonal):
    try:
        if not seasonal:
            trend, damped, boxcox, bias = cfg
            model = ExponentialSmoothing(np.asarray(train), trend=trend, damped=damped, seasonal=None) \
                .fit(optimized=True, use_boxcox=boxcox, remove_bias=bias)
        else:
            trend, damped, boxcox, bias, seasonal, seasonal_periods = cfg
            model = ExponentialSmoothing(np.asarray(train), trend=trend, damped=damped, seasonal=seasonal,
                                         seasonal_periods=seasonal_periods) \
                .fit(optimized=True, use_boxcox=boxcox, remove_bias=bias)

        return model.forecast(duration), model
    except NotImplementedError:
        raise NotImplementedError


def evaluateModel(train, test, cfg, seasonal):
    try:
        output, model = exponentialSmoothing(train, len(test), cfg, seasonal)
        return [output, sqrt(mean_squared_error(output, test)), cfg, model]
    except NotImplementedError:
        #print("Error", cfg)
        return [None, np.inf, None, None]
    except ValueError:
        #print("Error", cfg)
        return [None, np.inf, None, None]


def getconfiglist(seasonal=False):
    configlist = []
    trend = ["add", "mul", None]
    damped = [True, False]
    boxcox = [True, False]
    bias = [True, False]

    seasonalparam = ['add', 'mul']  # only for holtWinters
    seasonalperiod = [1]  # only for holtWinters

    # optimized = True
    for t in trend:
        for d in damped:
            for bx in boxcox:
                for b in bias:
                    if not seasonal:
                        cfg = [t, d, bx, b]
                        configlist.append(cfg)
                    else:
                        for s in seasonalparam:
                            for sp in seasonalperiod:
                                cfg = [t, d, bx, b, s, sp]
                                configlist.append(cfg)
    return configlist


def runTSA(train, test, configlist, seasonal, threading=True):
    if threading:
        if platform.system() == 'Linux':
            jobs = Parallel(n_jobs=ncpu, backend='multiprocessing')
        elif platform.system() == 'Windows':
            jobs = Parallel(n_jobs=ncpu, backend="threads")
        tasks = (delayed(evaluateModel)(train, test, cfg, seasonal) for cfg in configlist)
        results = jobs(tasks)
    else:
        results = [evaluateModel(train, test, cfg, seasonal) for cfg in configlist]
    return results


def main(correlationId, modelName, pageInfo, userId, seasonal=False,version=None):
    logger = utils.logger('Get', correlationId)
    start = time.time()
    try:
        utils.updQdb(correlationId, 'P', '10', pageInfo, userId, modelName=modelName, problemType='TimeSeries')
        data = utils.data_timeseries(correlationId, 'DE_PreProcessedData')
        DateCol = utils.getDateCol(correlationId)
        utils.logger(logger, correlationId, 'INFO',
                     ('Modelname ' + modelName + " Data Fetched at : " + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        test_size = 0.2

        freq, _, _ = utils.getTimeSeriesParams(correlationId)
        for i, selectedFreq in enumerate(freq):
            df = data[selectedFreq]
            df.set_index(df[DateCol], drop=True, inplace=True)
            df.index = pd.to_datetime(df.index)
            df.sort_index(inplace=True)

            _modelName = modelName + "_" + selectedFreq

            train, test = train_test_split(df, test_size=test_size, shuffle=False)
            # train = train.astype('double')
            # test = test.astype('double')
            utils.logger(logger, correlationId, 'INFO',
                         ('Modelname ' + _modelName  + " Test Train split at : " + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
            configlist = getconfiglist(seasonal)
            # print (train)
            try:
                results = runTSA(np.asarray(train[train.columns.difference([DateCol])]),
                                 np.asarray(test[test.columns.difference([DateCol])]), configlist, seasonal)
            except NotImplementedError:
                error_encounterd = 'NotImplementedError'
            scores = [result[1] for result in results]
            indx = scores.index(min(scores))
            forecastedValues, _, selectedConf, selectedModel = results[indx]
            selectedModel = ExponentialSmoothing(np.asarray(df[df.columns.difference([DateCol])]), trend=selectedConf[0], damped=selectedConf[1],seasonal=None).fit(optimized=True, use_boxcox=False, remove_bias=selectedConf[3])
            lastDataRecorded = test.index.strftime('%Y-%m-%d %H:%M:%S').astype('str').tolist()[-1]
            RangeTime = test.index.strftime('%d/%m/%Y %H:%M:%S').astype('str').tolist()
            #print(forecastedValues.tolist())
            r2ScoreVal, rmsVal, maeVal, mseVal = regression_evaluation.evaluate_reg(
                test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(), forecastedValues,
                multioutput=None, timeseries=True)
            end = time.time()
            utils.save_file((selectedModel, selectedConf), _modelName, 'TimeSeries', correlationId, pageInfo, userId,
                            list(train.columns), 'MLDL_Model',version=version)
            xlabelname = DateCol
            ylabelname = test.columns.difference([DateCol])[0]
            utils.insert_EvalMetrics_FI_T(correlationId, _modelName, 'TimeSeries', r2ScoreVal, rmsVal, maeVal, mseVal, end-start, test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(), forecastedValues.tolist(), RangeTime, selectedFreq, lastDataRecorded, xlabelname,ylabelname,"forecast", pageInfo, userId,version=version)
            updatevalue = int(10 + 89 * (i + 1) / len(freq))
            utils.updQdb(correlationId, 'P', str(updatevalue), pageInfo, userId, modelName=modelName,
                         problemType='TimeSeries')
            utils.logger(logger, correlationId, 'INFO',
                         ('Modelname ' + _modelName + " Model Created and Saved at : " + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.updQdb(correlationId, 'C', '100', pageInfo, userId, modelName=modelName, problemType='TimeSeries')
    except Exception as e:
        utils.updQdb(correlationId, 'E', str(e.args), pageInfo, userId, modelName=modelName, problemType='TimeSeries')
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(None))
        utils.save_Py_Logs(logger, correlationId)
    else:
        utils.logger(logger, correlationId, 'INFO',
                     ('Modelname ' + modelName + " Training Model completed at : " + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.save_Py_Logs(logger, correlationId)
