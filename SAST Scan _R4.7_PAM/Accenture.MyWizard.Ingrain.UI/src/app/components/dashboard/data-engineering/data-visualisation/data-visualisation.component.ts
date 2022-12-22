import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as d3 from 'd3';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';

@Component({
  selector: 'app-data-visualisation',
  templateUrl: './data-visualisation.component.html',
  styleUrls: ['./data-visualisation.component.scss']
})
export class DataVisualisationComponent implements OnInit {

  xaxis = '';
  yaxis = '';
  statistics = ['mean', 'median', 'min', 'max'];

  margins = { left: 50, top: 10, bottom: 50, right: 10 };
  width = 500 - this.margins.left - this.margins.right;
  height = 350 - this.margins.top - this.margins.bottom;
  processedObject: {} = {};
  selectedstat = 'mean';
  graphType: any;
  svg: any;
  xscale: any;
  yscale: any;
  xaxisName: any;
  yaxisName: any;
  xaxisGroup: any;
  yaxisGroup: any;
  data: any;
  dropDownsData: any;
  chartDropDownData: any;
  displayAxisDropDowns = 'false';
  axisNames: any;
  clonedAxisNames: any;
  isDisabled = true;

  constructor(private router: Router, private ns: NotificationService, private cus: CoreUtilsService) {

  }

  ngOnInit() {

    d3.json('assets/axisData.json').then(
      data => {
        this.dropDownsData = data;
        this.chartDropDownData = data.charts;
      }

    );

    this.svg = d3.select('#chart-area').append('svg')
      .attr('width', this.width + this.margins.left + this.margins.right)
      .attr('height', this.height + this.margins.bottom + this.margins.top)
      .append('g')
      .attr('transform', 'translate(' + this.margins.left + ',' + this.margins.top + ')');
  }

  changeStat(stat) {
    this.selectedstat = stat;
    this.updateGraph(this.data);
  }

  onAxisTypeSelect(value, axisType) {

    if (this.cus.isNil(value)) {
      this.ns.error(`Please Select valid ${axisType}`);
      this.isDisabled = true;
      this.clonedAxisNames.push(axisType);
    } else {
      this[axisType + 'Name'] = value;
      const matchedIndex = this.clonedAxisNames.indexOf(axisType);
      if (matchedIndex !== -1) {
        this.clonedAxisNames.splice(matchedIndex, 1);
      }

    }

    if (this.clonedAxisNames.length === 0) {
      this.isDisabled = false;
    }
  }

  onGraphTypeSelect(graphName) {
    this.isDisabled = true;
    this.axisNames = this.dropDownsData[graphName];
    this.clonedAxisNames = Object.assign([], this.axisNames);
    this.displayAxisDropDowns = 'true';
    this.graphType = graphName;
  }


  onSubmit() {
    if (this.xaxisName === this.yaxisName) {
      this.ns.error('both cant be same');
    }

    this.generateGraph(this.graphType);
  }


  generateGraph(graphType) {

    d3.json('assets/profitsData.json').then(
      da => {
        this.data = da.data;
        this.formatData(this.data);
        /// scales
        this.xscale = d3.scaleBand()
          .range([0, this.width])
          .paddingInner(0.2).paddingOuter(0.2);

        this.yscale = d3.scaleLinear()
          .range([this.height, 0]);

        // axises
        this.xaxisGroup = this.svg.append('g');
        this.yaxisGroup = this.svg.append('g');

        this.updateGraph(this.data);
        // rectangle
      });

  }

  updateGraph(data) {

    const domain = Array.from(this.processedObject['uniquesxaisvalues']);
    this.xscale.domain(domain);

    this.yscale.domain([0, d3.max(data.map(y => y[this.yaxisName]), t => +t)]);

    this.xaxis = d3.axisBottom(this.xscale);
    this.xaxisGroup.call(this.xaxis).attr('transform', 'translate(0,' + this.height + ')');

    this.yaxis = d3.axisLeft(this.yscale);
    this.yaxisGroup.call(this.yaxis);

    const rects = this.svg.selectAll('rect').data(domain);


    const line = d3.line()
      .x(d => this.xscale(d) + + this.xscale.bandwidth() / 2)
      .y(d => this.yscale(this.statisticFactory(d)));

    if (this.graphType === 'BarChart') {

      rects.attr('x', d => this.xscale(d))
        .attr('y', d => this.yscale(this.statisticFactory(d)))
        .attr('width', this.xscale.bandwidth)
        .attr('height', d => this.height - this.yscale(this.statisticFactory(d)))
        .attr('fill', 'blue');
      // ('After Update', rects)

      rects.exit().remove();

      // ('After exit', rects)
      rects.enter().append('rect')
        .attr('x', d => this.xscale(d))
        .attr('y', d => this.yscale(this.statisticFactory(d)))
        .attr('width', this.xscale.bandwidth)
        .attr('height', d => this.height - this.yscale(this.statisticFactory(d)))
        .attr('fill', 'blue');
      // ('After Initiallize', rects)

    }

    if (this.graphType === 'LineChart') {

      this.xscale = d3.scaleBand()
        .range([0, this.width])
        .paddingInner(0.2).paddingOuter(0.2);

      this.yscale = d3.scaleLinear()
        .range([this.height, 0]);

      // axises
      this.xaxisGroup = this.svg.append('g');
      this.yaxisGroup = this.svg.append('g');

      this.svg.selectAll('*').remove();

      this.svg.append('path')
        .datum(domain)
        .attr('fill', 'none')
        .attr('stroke', 'steelblue')
        .attr('stroke-linejoin', 'round')
        .attr('stroke-linecap', 'round')
        .attr('stroke-width', 1.5)
        .attr('d', line);
    }

  }

  formatData(data) {
    this.processedObject = {};
    this.processedObject['uniquesxaisvalues'] = new Set();
    data.forEach(element => {
      if (this.processedObject.hasOwnProperty(element[this.xaxisName])) {
        this.processedObject[element[this.xaxisName]].push(element[this.yaxisName]);
      } else {
        this.processedObject[element[this.xaxisName]] = [];
        this.processedObject[element[this.xaxisName]].push(element[this.yaxisName]);
        this.processedObject['uniquesxaisvalues'].add(element[this.xaxisName]);

      }

    });
  }


  statisticFactory(d) {

    if (this.selectedstat === 'mean') {
      return d3.mean(this.processedObject[d], x => +x);
    }
    if (this.selectedstat === 'median') {
      return d3.median(this.processedObject[d], x => +x);
    }

    if (this.selectedstat = 'min') {
      return d3.min(this.processedObject[d], x => +x);
    }
  }
}
