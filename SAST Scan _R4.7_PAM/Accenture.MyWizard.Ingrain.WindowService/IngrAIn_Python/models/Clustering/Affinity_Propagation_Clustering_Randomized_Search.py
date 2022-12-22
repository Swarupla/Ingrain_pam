# -*- coding: utf-8 -*-
"""
Created on Fri Jan 29 14:22:00 2019

@author: shrayani.mondal
"""

#import the libraries
#import statements
from sklearn.datasets import make_blobs
import numpy as np
import matplotlib.pyplot as plt
from sklearn.cluster import AffinityPropagation
from sklearn import metrics
import pickle
import json
from sklearn.model_selection import RandomizedSearchCV
from sklearn.metrics import make_scorer

#create clusters/blobs
data = make_blobs(n_samples=200, n_features=2, centers=4, cluster_std=1.6, random_state=50)
points = data[0]
labels_true = data[1]


#model definition
def ap_clustering_with_random_search(Data, myHyperparamsAffinityPropagation, hyperFlag):
    model = AffinityPropagation()
    try:
        if hyperFlag is True:
            ap_clust_model = AffinityPropagation(damping=myHyperparamsAffinityPropagation['damping'], max_iter=myHyperparamsAffinityPropagation['max_iter'], convergence_iter=myHyperparamsAffinityPropagation['convergence_iter'], copy=myHyperparamsAffinityPropagation['copy'], preference=myHyperparamsAffinityPropagation['preference'], affinity=myHyperparamsAffinityPropagation['affinity'], verbose=myHyperparamsAffinityPropagation['verbose'])
            ap_clust_model.fit(points)
            centers = ap_clust_model.cluster_centers_indices_
            labels = ap_clust_model.labels_
            n_clusters = len(centers)
            homogeneity = metrics.homogeneity_score(labels_true, labels)
            completeness = metrics.completeness_score(labels_true, labels)
            v_measure = metrics.v_measure_score(labels_true, labels)
            adjusted_rand_index = metrics.adjusted_rand_score(labels_true, labels)
            adjusted_mutual_info = metrics.adjusted_mutual_info_score(labels_true, labels)
            outputScore = {"hyperParams" : myHyperparamsAffinityPropagation, "EstimatedNumberOfClusters" : n_clusters, "Homogeneity" : homogeneity, "Completeness" : completeness, "V_Measure" : v_measure, "Adjusted_Rand_Index" : adjusted_rand_index, "Adjusted_Mutual_Info" : adjusted_mutual_info}
            pickle.dump(ap_clust_model, open("ap_clust_model.pickel", "wb"))
        else:
            max_iter = [50,100,200,300,400,500]
            convergence_iter = [5,10,15,20,25]
            preference = [None, -50, -100, -150, -200, -250, -300, -350, -400]
            affinity = ["euclidean"]
            SCORERS = {'homogeneity':make_scorer(metrics.homogeneity_score), 'completeness':make_scorer(metrics.completeness_score), 'v_measure':make_scorer(metrics.v_measure_score), 'adjusted_rand_index':make_scorer(metrics.adjusted_rand_score), 'adjusted_mutual_info':make_scorer(metrics.adjusted_mutual_info_score)}
            ap_clust_random_model = RandomizedSearchCV(estimator = model, param_distributions = dict(max_iter = max_iter, convergence_iter = convergence_iter, preference = preference, affinity = affinity), refit='homogeneity', n_iter = 100, scoring = SCORERS, verbose = 2, random_state = 42, n_jobs = -1)
            ap_clust_random_model.fit(points,labels_true)
            outputScore = {"hyperParams":ap_clust_random_model.best_params_, "AffinityPropagationClustering_Score":ap_clust_random_model.best_score_}
            pickle.dump(ap_clust_random_model, open("ap_clust_random_model.pickel", "wb"))

        outputScoreJson = json.dumps(outputScore)
        return outputScoreJson
    except Exception as e:
        error = {"error": e}
        errorJson = json.dumps(error)
        return errorJson