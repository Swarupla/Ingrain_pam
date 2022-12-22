import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-d3-donut-chart',
  templateUrl: './d3-donut-chart.component.html',
  styleUrls: ['./d3-donut-chart.component.scss']
})
export class D3DonutChartComponent implements AfterViewInit, OnInit, OnChanges {
  @ViewChild('containerPieChart', { static: true }) element: ElementRef;
  @Output() arcClicked = new EventEmitter<string>();

  constructor() { }

  radius: number;
  svg;
  color;
  pie;
  data_ready;
  htmlElement: HTMLElement;
  host;
  accuracyValue;
  public data = {};

  @Input() donutInnerRadius: number;
  donutHeight: number;
  donutWidth: number;
  donutMargin: number;
  tooltipDiv; // Asset Usage

  @Input() pageInfoForDonutChart: string;
  @Input() trainedModelAccuracy: number;
  @Input() selectedModelAccuracy: number;
  @Input() pythonProgressVal;
  @Input() selectedModelr2Values;
  @Input() defaultModelAccuracyOnLoad;
  @Output() selectedAIModelAccuracy = new EventEmitter<string>();
  @Input() selectedModelOnLoad;
  @Input() appName: string;
  @Input() dataForAssetUsage;
  ngOnInit() { }

  ngAfterViewInit() {
    this.htmlElement = this.element.nativeElement;
    this.host = d3.select(this.htmlElement);
    this.setup();
    this.buildSVG();
    this.draw();
  }

  ngOnChanges() {
    this.ngAfterViewInit();
  }

  setup() {
    if (this.pageInfoForDonutChart === 'Accuracy' || this.pageInfoForDonutChart === 'RegressionR2Value'
      || this.pageInfoForDonutChart === 'RegressionMSEValue' || this.pageInfoForDonutChart === 'TimeSeriesValue') {
      this.donutHeight = 120;
      this.donutWidth = 150;
      this.donutMargin = 20;
    } else if (this.pageInfoForDonutChart === 'StartTrainning' || this.pageInfoForDonutChart === 'CompareTest') {
      this.donutHeight = 90;
      this.donutWidth = 90;
      this.donutMargin = 15;
    } else if (this.pageInfoForDonutChart === 'AssetUsage') {
      this.donutHeight = 400;
      this.donutWidth = 500;
      this.donutMargin = 40;
    } else if ( this.pageInfoForDonutChart === 'ADTrainedModel'){
      this.donutHeight = 110;
      this.donutWidth = 120;
      this.donutMargin = 15;
    }else {
      this.donutHeight = 100; // 80;
      this.donutWidth = 140; // 80;
      this.donutMargin = 15; // 10;
    }
    this.radius = Math.min(this.donutWidth, this.donutHeight) / 2 - this.donutMargin;

    // if (this.pageInfoForDonutChart === 'StartTrainning') {
    //   this.trainedModelAccuracy = 0;
    //   this.donutHeight = 120;
    //   this.donutWidth = 120;
    // }
  }

  buildSVG() {
    this.host.html('');
    if (this.pageInfoForDonutChart === 'AssetUsage') {
      this.tooltipDiv = this.host.append('div').attr('class', 'tooltipInnerDiv')
        .style('font-size', '0.7rem')
        .style('background-color', 'rgba(0, 0, 0, 0.8)')
        .style('color', 'white');
    }

    this.svg = this.host.append('svg')
      .attr('width', this.donutWidth)
      .attr('height', this.donutHeight)
      .append('g')
      .attr('transform', 'translate(' + this.donutWidth / 2 + ',' +
        this.donutHeight / 2 + ')');


  }

  draw() {
    const _this = this;

    // Compute the position of each group on the pie and build pie:
    // Bug raised by North American team
    // Desc :- The direction of R2 value circle chart is sometimes inconsistent
    // Fix :- add .sort(null)
    if (this.pageInfoForDonutChart === 'AssetUsage') {
      this.pie = d3.pie()
        .sort(null)
        .value(function (d) { return d.value; })
        .padAngle(.001);
    } else {
      this.pie = d3.pie()
        .sort(null)
        .value(function (d) { return d.value; });
    }

    if (this.pythonProgressVal >= 0) {
      this.createDataSourceBindText(this.pythonProgressVal);
    } else if (this.trainedModelAccuracy !== undefined) {
      this.createDataSourceBindText(this.trainedModelAccuracy);
    } else if (this.selectedModelAccuracy !== undefined) {
      this.createDataSourceBindText(this.selectedModelAccuracy);
    } else if (this.selectedModelr2Values !== undefined) {
      this.createDataSourceBindText(this.selectedModelr2Values);
    } else if (this.defaultModelAccuracyOnLoad !== undefined) {
      this.createDataSourceBindText(this.defaultModelAccuracyOnLoad);
    }

    if (this.pageInfoForDonutChart === 'AssetUsage') {
      this.data = this.dataForAssetUsage; // { release_planner : 20, instaMLfeatre: 60 , threeesdsandjaja: 20};
      this.svg.append('text')
        .attr('dy', '0em')
        .attr('text-anchor', 'middle')
        .attr('font-size', '0.7rem')
        .attr('font-weight', 'bold')
        .text(this.appName);
      // set the color scale
      this.color = d3.scaleOrdinal(d3.schemePaired);
    }

    this.data_ready = this.pie(d3.entries(this.data));


    // populte pie/donut
    this.svg
      .selectAll('whatever')
      .data(this.data_ready)
      .enter()
      .append('path')
      .attr('class', 'donutArc')
      .attr('id', function (d, i) { return 'donutArc_' + i; }) // Unique id for each slice
      .attr('d', d3.arc()
        .innerRadius(this.donutInnerRadius)// This is the size of the donut hole
        .outerRadius(this.radius))
      .attr('fill', (d) => (this.color(d.data.key)))
      .attr('stroke', '#3078b3')
      .style('stroke-width', '0px')
      .style('opacity', 0.7)
      .style('cursor', 'pointer')
      .on('click', function (d, i) {
        _this.accuracyValue = d.data.value;
        _this.arcClicked.emit(d.data);
      });

    if (this.pageInfoForDonutChart === 'AssetUsage') {
      this.donutWithLabelsAssetUsage(_this);
    }
  }

  accuracyDivLoading() {
    this.selectedAIModelAccuracy.emit(this.accuracyValue);
  }

  createDataSourceBindText(values) {
    if (this.selectedModelOnLoad === 'regression' || this.selectedModelOnLoad === 'Regression'
      || this.selectedModelr2Values !== undefined) {
      this.data = { a: values, b: 100 - values };

      this.svg.append('text')
        .attr('text-anchor', 'middle')
        .text(values);
    } else if (this.selectedModelOnLoad === 'classification' || this.selectedModelOnLoad === 'Classification') {
      this.data = { a: values, b: 100 - values };
      this.svg.append('text')
        .attr('text-anchor', 'middle')
        .text(values + ' %');

    } else if (this.pageInfoForDonutChart === 'StartTrainning' || this.pageInfoForDonutChart === 'CompareTest') {
      this.data = { a: values, b: 100 - values };
      this.svg.append('text')
        .attr('dy', '0em')
        .attr('text-anchor', 'middle')
        .attr('font-size', '0.7rem')
        .attr('font-weight', 'bold')
        .text(values + '%');
      // .text(function (d) {
      //   return values + '%';
      // });
      if (this.pageInfoForDonutChart === 'StartTrainning') {
        this.svg.append('text')
          .attr('dy', '1.5em')
          .style('text-anchor', 'middle')
          .attr('font-size', '0.56rem')          
          .text('Completed');
          
      } else if (this.pageInfoForDonutChart === 'CompareTest') {
        this.svg.append('text')
          .attr('dy', '1.5em')
          .style('text-anchor', 'middle')
          .attr('font-size', '0.56rem')
          .text('Accuracy');
      }

    } else {
      this.data = { a: values, b: 100 - values };
      this.svg.append('text')
        .attr('text-anchor', 'middle')
        .text(values + ' %');
    }

    // set the color scale
    this.color = d3.scaleOrdinal()
      .domain(Object.keys(this.data))
      .range(['#2887cc', '#b8c0ca']);
  }



  donutWithLabelsAssetUsage(_this) {

    const label = this.svg.selectAll('.donutArc')
      .append('title')
      .text(function (d) {
        return d.data.key + ': ' + d.data.value;
      });
    // .on('mouseover', function (d) {
    //   _this.tooltipDiv.transition()
    //     .duration(400)
    //     .style('opacity', .9);
    //   _this.tooltipDiv.html(d.data.key + ' - ' + d.data.value)
    //     .style('left', _this.setLeftfeature(d3.event))
    //     .style('position', 'absolute')
    //     .style('padding', '1em')
    //     .style('width','20em')
    //     .style('top', _this.setTopfeature(d3.event));
    // })
    // .on('mouseout', function (d) {
    //   _this.tooltipDiv.transition()
    //     .duration(500)
    //     .style('opacity', 0);
    // });
  }

  setLeftfeature(event) {
    let left;
    left = (event.layerX) + 'px';
    return left;
  }
  setTopfeature(event) {
    let top;
    top = (event.layerY) + 'px';
    return top;
  }
}
