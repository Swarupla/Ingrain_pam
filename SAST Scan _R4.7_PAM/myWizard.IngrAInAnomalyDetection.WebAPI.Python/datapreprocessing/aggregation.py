# -*- coding: utf-8 -*-
"""
Created on Mon Jun 17 16:09:22 2019

@author: nitin.john.james
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
import numpy as np
from SSAIutils import utils

freqDict = {"Yearly": "Y", "Hourly": "H", "Daily": "D", "CustomDays": "D", "Weekly": "W",
            "Monthly": "M", "Quarterly": "Q",
             "Half-Year": "6M", "Fortnightly": "2W"}

'''Count Aggregation: Counts the number of rows in the given column at a given frequency'''


def countFunc(df, frequency, col_to_aggr,value):
    countDF = groupBy(df,
                      frequency,value).count()  # uses the return value of custom groupBy function below(type is groupby object) to count
    countDF=countDF.rename(columns={col_to_aggr:col_to_aggr +"_"+ "count"}) #renames count values column to proper syntax
    countDF = countDF.replace(0, np.nan).dropna()

    return countDF  # returns the countDF, which only contains the Date column as the index and the count of rows according to frequency


''' Maximum Aggregation: Calculates the max number at a given frequency for numeric datatypes and gives which value appears the most in a given frequency for string datatypes'''


def maximum(df, frequency, col_to_aggr, func, value):
    if df[col_to_aggr].dtypes == 'object':  # if the column has data type of string, do the below
        maxDF = minmaxFunc(df, frequency, col_to_aggr, func, value)

    else:  # if col_to_aggr is a numeric one
        maxDF = groupBy(df, frequency,value).max()  # find the max value in a given frequency
        maxDF = maxDF.rename(columns={
            col_to_aggr: col_to_aggr + '_max'})  # renames value column to just the name of the <column_function>
        maxDF = maxDF.replace(0, np.nan).dropna()

    return maxDF  # returns the maxDF with just date as index and the max of col_to_aggr. To be aggregated into one df below


''' Minimum Aggregation: Calculates the min number at a given frequency for numeric datatypes and gives which value appears the least in a given frequency for string datatypes'''


def minimum(df, frequency, col_to_aggr, func, value):

    if df[col_to_aggr].dtypes == 'object':  # if the column has data type of string, get the word that appears the least
        minDF = minmaxFunc(df, frequency, col_to_aggr,func, value)

    else:
        minDF = groupBy(df, frequency,value).min()  # call custom groupBy(returns groupby object) and do min() on it
        minDF = minDF.rename(columns={col_to_aggr: col_to_aggr + "_min"})
        minDF = minDF.replace(0, np.nan).dropna()

    return minDF  # returns minDF to be aggregated below

def minmaxFunc(df, frequency, col_to_aggr, func, value):
    dateCol = df.index.name  # get name of index as dateCol
    # dictionary of frequencies and their aliases
    freqAlias = freqDict.get(frequency, None) #gets the frequency of the dictionary
    if frequency == "CustomDays":
        freqAlias = str(value)+freqAlias
    timeseries_comp = pd.DataFrame(pd.date_range(df.index.min(), df.index.max(), freq=freqAlias)).set_index(0)#fills in missing dates according to frequency
    df = pd.merge(timeseries_comp, df, left_index=True, right_index=True,how='outer') #merges timeseries and original df
    df2 = groupBy(df, frequency, value)[col_to_aggr].value_counts().reset_index(
        name='count').rename(columns = {"level_0":dateCol})  # resets date as column and gives a numerical index while adding count as a column
    if func == 'min':
    
        df3 = df2[[dateCol, 'count']].groupby(dateCol).min().reset_index()  # group by date and count and find the max
    elif func == 'max': 
        df3 = df2[[dateCol, 'count']].groupby(dateCol).max().reset_index()  # group by date and count and find the max

    mergeDF = pd.merge(df2, df3, how='inner', on=[dateCol, 'count'])  # inner join
    mergeDF = mergeDF.groupby(['count', dateCol])[col_to_aggr].apply(
        ','.join).reset_index()  # groupby date and count again and get the ones with same count into a list using .join with a comma
    colList = [dateCol, col_to_aggr, 'count']  # order of columns needed
    mergeDF = mergeDF[colList].set_index(dateCol)  # reorder mergeDF columns into the order in line 186
    if func == 'min':
        minmaxDF = mergeDF.rename(columns={col_to_aggr: col_to_aggr + "_min",
                                    'count': col_to_aggr + '_min_count'}).sort_index()  # rename the columns as needed
    
    else:
        minmaxDF = mergeDF.rename(columns={col_to_aggr: col_to_aggr + "_max",
                                    'count': col_to_aggr + '_max_count'}).sort_index()  # rename the columns as needed
    minmaxDF = minmaxDF.dropna()
    minmaxDF = minmaxDF.replace(0, np.nan).dropna()  
    return minmaxDF


'''Sum Aggregation: Sums the contents of a specific column according to frequency'''
    

def summation(df, frequency, col_to_aggr,value):
    sumDF = groupBy(df, frequency, value).sum()  # call custom groupBy(returns groupby object) and do sum() on it
    sumDF = sumDF.rename(columns={col_to_aggr: col_to_aggr + "_" + 'sum'})  # rename col_to_aggr to <col_to_aggr+sum>
    sumDF = sumDF.replace(0, np.nan).dropna()

    return sumDF  # returns sumDF to be aggregated below


'''Product Agggregation: Multiplies the contents of specific column according to frequency. Returns inf values in column if product value is too large for int64 datatype'''


def product(df, frequency, col_to_aggr, value):
    prodDF = groupBy(df, frequency, value).prod()  # call custom groupBy(returns groupby object) and do prod() on it
    prodDF = prodDF.rename(
        columns={col_to_aggr: col_to_aggr + "_" + 'product'})  # rename col_to_aggr to <col_to_aggr+prod>
    prodDF = prodDF.replace(0, np.nan).dropna()

    return prodDF  # returns prodDF to be aggregated below


'''Mean Aggregation: Calculates mean for chosen column of numeric type '''


def mean(df, frequency, col_to_aggr, value):
    #print (df,frequency)
    meanDF = groupBy(df, frequency, value).mean()
    meanDF[col_to_aggr] = meanDF[col_to_aggr].round(2)
    meanDF = meanDF.rename(columns={col_to_aggr: col_to_aggr + "_" + 'mean'})
    meanDF = meanDF.replace(0, np.nan).dropna()

    return meanDF


'''Median Aggregation: Calculates median for chosen column of numeric type '''


def median(df, frequency, col_to_aggr, value):
    medianDF = groupBy(df,
                       frequency,value).median()  # performs median function on return value of groupBy function(groupBy object)
    medianDF[col_to_aggr] = medianDF[col_to_aggr].round(2)  # rounds median values to two decimal places
    medianDF = medianDF.rename(
        columns={col_to_aggr: col_to_aggr + "_" + 'median'})  # renames median value column to <col_to_aggr+median>
    medianDF = medianDF.replace(0, np.nan).dropna()

    return medianDF  # returns medianDF to be aggregated below


'''Label Counts Aggregation: Calculates how many times each value(only works for object/string datatypes) appears in a given frequency'''


def label_counts(df, frequency, col_to_aggr, value):
    #dateCol = df.index.name
    # dictionary of frequencies and their aliases
    freqAlias = freqDict.get(frequency, None)  # gets the specific alias for the chosen frequency
    if frequency == "CustomDays":
        freqAlias = str(value)+freqAlias    
    timeseries_comp = pd.DataFrame(pd.date_range(df.index.min(), df.index.max(), freq=freqAlias)).set_index(0)#fills in missing dates according to frequency
    df = pd.merge(timeseries_comp, df, left_index=True, right_index=True,how='outer')
    label_countDF = groupBy(df, frequency,value)[col_to_aggr].count().reset_index().rename(columns={col_to_aggr:col_to_aggr+"_LabelCount"})
#    label_countDF = groupBy(df, frequency)[col_to_aggr].value_counts().reset_index(
#        name='count').rename(columns = {"level_0":dateCol})
    
    #adds a count column from value_count(returns a series) and converts to dataframe
    #label_countDF=label_countDF.pivot(index=dateCol, columns = col_to_aggr, values= 'count').fillna(0)
    #pivots and fills NaN with 0 and resets date as index
#    for colName in list(label_countDF.columns.values):
#        label_countDF = label_countDF.rename(columns=
#                                             {colName: colName + "_LabelCount"})
    return label_countDF  # returns dataframe for label_counts


''' Range Aggregation: Calculates the max and min for numeric values and the mode and least occured values for string values'''


def rangeDiv(df, frequency, col_to_aggr, func, value):
    func = 'min'
    minDF = minimum(df, frequency, col_to_aggr, func, value)  # call minimum functions
    func = 'max'
    maxDF = maximum(df, frequency, col_to_aggr, func, value)  # call maximum functions

    if df[col_to_aggr].dtypes != 'object':
        minmaxDF = pd.merge(minDF, maxDF, left_index=True, right_index=True, how='outer') #merges both min and max into one dataframe
        minmaxDF = minmaxDF.rename(columns={col_to_aggr + "_min": col_to_aggr + "_range_min",
                                            col_to_aggr + "_max": col_to_aggr + "_range_max"})#renames min and max columns



    elif df[col_to_aggr].dtypes == 'object':
        minDF = minDF.drop(col_to_aggr + '_min_count', axis=1) #drops the count column
        maxDF = maxDF.drop(col_to_aggr + '_max_count', axis=1) #drops the count column
        minDF = minDF.rename(columns={col_to_aggr + "_min": col_to_aggr + "_range_min"}) #renames columns
        maxDF = maxDF.rename(columns={col_to_aggr + "_max": col_to_aggr + "_range_max"})#renames columns
        minmaxDF = pd.merge(minDF, maxDF, left_index=True, right_index=True, how='outer')#merges columns into one dataframe

    return minmaxDF


# GROUPBY FUNCTION
'''Groups dataframe by chosen frequency and returns a groupby object to be used with aggregation function'''


def groupBy(df, frequency, value):
        # dictionary of frequencies and their aliases
    freqAlias = freqDict.get(frequency, None)  # gets the specific alias for the chosen frequency
    if frequency == "CustomDays":
        freqAlias = str(value)+freqAlias 
    df.groupby(pd.Grouper(freq=freqAlias))  # does groupby on dataframe and returns a groupby object
    return df.groupby(pd.Grouper(freq=freqAlias))  # returns a groupby object

def divideData(df, fields_to_aggr, functions_to_apply, frequency, dateCol, value):
    #df = df.set_index(dateCol)  # setting date column as index
    aggrDF = pd.DataFrame()  # make blank dataframe to be aggregated to below
    

    for num in range(0, len(fields_to_aggr)):
        expDFCol = df[[fields_to_aggr[
                           num]]]  # cuts down original dataframe to just the date as index and particular column in fields_to_aggr. Changes with each time for loop loops around

        if 'count' in functions_to_apply[num]:  # count is in specified index in functions_to_apply
            countDF = countFunc(expDFCol, frequency, fields_to_aggr[num],value)  # calls count() function
            countDF = countDF.rename(columns={fields_to_aggr[num]: fields_to_aggr[num] + "_" + functions_to_apply[
                num]})  # renames count values column to proper syntax
            aggrDF = pd.merge(aggrDF, countDF, left_index=True, right_index=True,
                              how='outer')  # aggregates to aggrDF dataframe so all results of aggregate functions can be in the same dataframe

        elif 'max' in functions_to_apply[num]:  # 'max' is specified in index in functions to apply
            func = functions_to_apply[num]
            maxDF = maximum(expDFCol, frequency, fields_to_aggr[num], func, value)  # calls maxDF()
            aggrDF = pd.merge(aggrDF, maxDF, left_index=True, right_index=True,
                              how='outer')  # aggregates to aggrDF dataframe so all results of aggregate functions can be in the same dataframe
        elif 'min' in functions_to_apply[num]:  # 'min' is specified in index in functions to apply
            func = functions_to_apply[num]
            minDF = minimum(expDFCol, frequency,fields_to_aggr[num],func, value)

            aggrDF = pd.merge(aggrDF, minDF, left_index=True, right_index=True,
                              how='outer')  # aggregates to aggrDF dataframe so all results of aggregate functions can be in the same dataframe
        elif 'sum' in functions_to_apply[num]:  # 'sum/prod' is specified in index in functions to apply

            sumDF = summation(expDFCol, frequency, fields_to_aggr[num], value)  # call sum() function
            aggrDF = pd.merge(aggrDF, sumDF, left_index=True, right_index=True,
                              how='outer')  # merges sumDF into aggrDF so all results of aggregate functions can be in the same dataframe
        elif 'product' in functions_to_apply[num]:  # 'prod' is specified in index in functions to apply
            prodDF = product(expDFCol, frequency, fields_to_aggr[num], value)  # call  prod() function
            aggrDF = pd.merge(aggrDF, prodDF, left_index=True, right_index=True,
                              how='outer')  # merges prodDF into aggrDF so all results of aggregate functions can be in the same dataframe
        elif 'mean' in functions_to_apply[num]:  # 'mean' is specified in index in functions to apply
            meanDF = mean(expDFCol, frequency, fields_to_aggr[num], value)  # call mean() function
            aggrDF = pd.merge(aggrDF, meanDF, left_index=True, right_index=True,
                              how='outer')  # merges meanmedianDF into aggrDF so all results of aggregate functions can be in the same dataframe
        elif 'median' in functions_to_apply[num]:  # 'median' is specified in index in functions to apply
            medianDF = median(expDFCol, frequency, fields_to_aggr[num], value)  # call median() function
            aggrDF = pd.merge(aggrDF, medianDF, left_index=True, right_index=True,
                              how='outer')  # #merges medianDF into aggrDF so all results of aggregate functions can be in the same dataframe
        elif 'labelcounts' in functions_to_apply[num]:  # 'LabelCounts' is specified in index in functions to apply
            label_countDF = label_counts(expDFCol, frequency, fields_to_aggr[num], value)  # call label_counts() function
            aggrDF = pd.merge(aggrDF, label_countDF, left_index=True, right_index=True, 
                              how='outer')
        elif 'range' in functions_to_apply[num]:  # 'Range' is speicified in index of functions to apply
            func = functions_to_apply[num]
            minmaxAggrDF = rangeDiv(expDFCol, frequency, fields_to_aggr[num], func, value)  # calls rangeDiv() function
            aggrDF = pd.merge(aggrDF, minmaxAggrDF, left_index=True, right_index=True,
                              how='outer')  # merges minmaxAggrDF to aggrDF so all results of aggregate functions can be in the same dataframe


    return aggrDF  # returns aggrDF

def main(correlationId,data,dateCol,targetCol,interpolationtechnique,freq=None,agg=None):
    if not freq:
        freq,agg,_ = utils.getTimeSeriesParams(correlationId)
        agg = agg.lower()
    try:
        data[dateCol]= pd.to_datetime(data[dateCol],dayfirst = True, errors='coerce')
    except:
        data[dateCol] = pd.to_datetime(data[dateCol], utc=True)
        #data[dateCol]= pd.to_datetime(data[dateCol],errors='coerce')  
    data.dropna(subset=[dateCol],inplace=True)
    
    data.set_index(dateCol,inplace=True)
    #data.index = pd.to_datetime(data.index)
    
    data.sort_index(inplace=True)
    #if data[targetCol].dtype.name in ['int64','float64'] :
     #   data = data.loc[~data.index.duplicated(keep='first')]
     #Error Definitions
    collatedData={}
    
##       selectedFreq = freq[0]
##       freqc= freqDict.get(selectedFreq, None)        
##       collatedData[selectedFreq] = data
##       return collatedData
       
    NumStrerr = "Unable to ", agg, "on data-types that are not string or numbers.\
    The column called ", targetCol, " is not numeric or string.Please choose a \
    column with numeric or string data-type"
    
    Numerr = "Unable to ", agg, " on non-numeric data-types. The column called ", \
                     targetCol, " is not numeric.Please choose a column with \
                     numeric data-type"
    Strerr = "Unable to ", agg, " on non-string data-types. The column called ", \
                    targetCol, " is not string.Please choose a column with string data-type"                         
                   
    for selectedFreq  in freq:
        #if data[targetCol].dtype.name in ['int64','float64'] :
        #    data_resampled = data.resample(freqDict[selectedFreq])
        #    df = data_resampled.interpolate(method=interpolationtechnique)
        #    df.fillna(value=0.0,inplace=True)
        #else:
        freqc= freqDict.get(selectedFreq, None) 
        #print ("here",selectedFreq)
        if selectedFreq == "CustomDays":
            freqc = str(value)+freqc
            v=value            
        else:
            v=None
            #print ("hereee")
        df = data
        if agg!="none":               
            if not df[targetCol].dtype.name in ['object','int32','int64','float64']: 
                        raise TypeError(NumStrerr)  
            else:
                if agg in ['sum','product','mean','median']: 
                    if not df[targetCol].dtype.name in ['int64','float64'] :
                        raise TypeError(Numerr)
                elif agg == 'labelcounts':  # 'LabelCounts' is specified in index in functions to apply
                    if df[targetCol].dtype.name not in ['int64','float64','int32','object']:  # checks if column is an object datatype
                        raise TypeError(Strerr)      
            
            
            df = divideData(df, [targetCol], [agg], selectedFreq, dateCol, v) 
            dateIndexRange = pd.date_range(start=df.index.min(), end=df.index.max(), freq=freqc)
            df = df.reindex(dateIndexRange).interpolate(method=interpolationtechnique.lower())
            df.sort_index(inplace=True)
            df.index.name = dateCol        
        #df = df.interpolate(method=interpolationtechnique.lower())
        collatedData [selectedFreq] = df
        print (collatedData)
    return collatedData
