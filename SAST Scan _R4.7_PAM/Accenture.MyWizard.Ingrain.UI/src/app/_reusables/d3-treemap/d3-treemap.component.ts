/***
 * Usage Pass Input data as below structure
 * data = {
     'children': [
     { 'Name': 'Olives', 'Count': 4319 },
     { 'Name': 'Tea', 'Count': 4159 } ...]
   };
 * Based on D3 Treemap function it will create Treemap structure chart on data provided
 */

import { Component, ElementRef, OnInit, ViewChild, Input } from '@angular/core';
import * as d3 from 'd3';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-d3-treemap',
  templateUrl: './d3-treemap.component.html',
  styleUrls: ['./d3-treemap.component.scss']
})
export class D3TreemapComponent implements OnInit {

  DEFAULT_CLUSTER_COLORS = [
    '#60607f',
    '#6d6acc',
    '#7188dc',
    '#67b7dc',
    '#67cabd',
    '#dcc467',
    '#d7a267',
    '#f3a6cd',
    '#e769a8',
    '#dcdc67',
    '#ed88bb',
    '#dc67ce',
    '#7927cc',
    '#8fd399',
    '#b7dc75'
  ];

  @ViewChild('containerTreeMapChart', { static: true }) element: ElementRef;
  
  @Input() data;
  @Input() setOfWordCloud; // { 'output': 'blobimage' , 'message': 'Success'}
  constructor(private ns: NotificationService) { }
  htmlElement: HTMLElement;
  host;
  width = 700;

  selectedWCImage = '';
  highLightSelectedBlock;

  // Line chart data
  lineChart = [];
  isRegression;
  lineChartDataCount;
  visualizationResponse;
  dddata = {
    'Forecast': [],
    'RangeTime': [],
    'xlabel': [
      'Sprint 37',
      'Sprint 14',
      'Sprint 48',
      'Sprint 28',
      'Sprint 38'
    ],
    'predictionproba': [
      91.31555938622313,
      26.241951253236884,
      79.58967894149353,
      -4.860558445045534,
      43.79645407089355
    ],
    'legend': [
      'Completed Story points '
    ],
    'target': 'Completed Story points ',
    'ProblemType': 'regression',
    'Target': null,
    'Frequency': null,
    'xlabelname': 'Sprint',
    'ylabelname': 'Completed Story points ',
    'BusinessProblems': 'test',
    'ModelName': 'test1cl2',
    'DataSource': 'Defect Prediction V1.csv',
    'Category': 'Agile'
  };
  // Line chart data

  ngOnInit() {
  }

  ngAfterViewInit() {
    this.htmlElement = this.element.nativeElement;
    this.host = d3.select(this.htmlElement);
    this.drawTreeMapChart();
    // this.createDataForRegression(this.dddata); to be used for next requirement

  }

  ngOnChanges() {
    this.ngAfterViewInit();
  }


  drawTreeMapChart(showWordCloud?) {
    let adjustedSize = this.width + 500;
    if (showWordCloud) {
      this.width = 200;
      adjustedSize = this.width + 500;
    }
    const color = d3.scaleOrdinal(d3.schemePaired);

    const treeMapS = d3.treemap(this.data)
      .tile(d3.treemapSquarify.ratio(1))
      .size([adjustedSize, 350])
      .padding(2);

    this.host.html('');

    const svg = this.host
      .append('svg')
      .attr('width', adjustedSize)
      .attr('height', 350)
      .attr('class', 'treeMap');

    const nodes = d3.hierarchy(this.data)
      .sum(function (d) { return d.Count; });
    const _this = this;

    svg.selectAll('rect')
      .data(treeMapS(nodes).leaves())
      .enter()
      .append('rect')
      .attr('x', function (d) {
        return d.x0;
      })
      .attr('y', function (d) {
        return d.y0;
      })
      .attr('width', function (d) {
        return d.x1 - d.x0;
      })
      .attr('height', function (d) {
        return d.y1 - d.y0;
      })
      .style('stroke', function (d, i) {
        if (_this.highLightSelectedBlock && d.data.Name === _this.highLightSelectedBlock) {
          return 'black';
        } else {
          return 'lightgrey';
        }
      })
      .style('fill', function (d, i) {
        return _this.DEFAULT_CLUSTER_COLORS[i];
      })
      .style('opacity', function (d, i) {
        if (_this.highLightSelectedBlock && d.data.Name !== _this.highLightSelectedBlock) {
          return '0.4';
        }
      })
      .style('cursor', 'pointer')
      .on('click', (d) => {
        if (_this.setOfWordCloud[d.data.Name]['output'] !== '') {
          _this.selectedWCImage = this.setOfWordCloud[d.data.Name]['output'];
        } else {
          _this.ns.warning(this.setOfWordCloud[d.data.Name]['message']);
        }
        _this.highLightSelectedBlock = d.data.Name;
        _this.drawTreeMapChart('ShowWordCloud');
      });



    svg.selectAll('rect')
      .append('title')
      .text(function (d) {
        return d.data.Name + ': ' + d.data.Count;
      });

    svg.selectAll('text')
      .data(treeMapS(nodes).leaves())
      .enter()
      .append('text')
      .selectAll('tspan')
      .data(d => {
        return (d.data.Name + ' ' + d.data.Count).split(' ') // split name
          .map(v => {
            return {
              text: v,
              x0: d.x0,
              y0: d.y0,
              width: d.x1 - d.x0,
              height: d.y1 - d.y0,
              count: (d.data.Name + ' ' + d.data.Count).split(' ').length,
              fullText: d.data.Name + ' ' + d.data.Count
            }
          });
      })
      .enter()
      .append('tspan')
      .attr('x', (d) => d.x0 + 5)
      .attr('y', (d, i) => d.y0 + 15 + (i * 15))       // offset by index
      .text((d, i) => {
        const v = d;
        if (d.height >= (i + 1) * 14) {
          if (d.width < d.text.length * 6) {
            return d.text.substring(0, d.text.length / 2) + '..';
          } else {
            return d.text;
          }
        }
      })
      .attr('font-size', function (d, i) {
        if (i === d.count - 1) {
          return '1em';
        } else { return '1em'; }
      })
      .attr('fill', 'white');


    // svg.selectAll('rect')
    // .attr('y', 0)
    // .attr('height', 0)
    // .transition()
    // .delay(function (d) {return Math.random() * 1000; })
    // .duration(1000)
    // .attr('y', function(d) { return d.y0; })
    // .attr('height', function(d) { return d.y1 - d.y0; });


    // d3.select(self.frameElement)
    //   .style('height', this.width + 100 + 'px');
  }

  // To be used for next requirement
  createDataForRegression(response) {
    this.visualizationResponse = response;
    for (let index = 0; index < response.predictionproba.length; index++) {
      const objLineChart = {};
      objLineChart['XAxis'] = response.xlabel[index];
      objLineChart['YAxis'] = response.predictionproba[index];
      this.lineChart[index] = objLineChart;
      this.isRegression = true;
    }
    this.lineChartDataCount = this.lineChart.length;
  }
}
