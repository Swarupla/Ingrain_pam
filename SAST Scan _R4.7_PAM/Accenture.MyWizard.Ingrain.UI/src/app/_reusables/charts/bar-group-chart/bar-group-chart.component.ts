import { Component, OnInit, Input, ViewChild, ElementRef } from '@angular/core';
import * as d3 from 'd3';
@Component({
  selector: 'app-bar-group-chart',
  templateUrl: './bar-group-chart.component.html',
  styleUrls: ['./bar-group-chart.component.scss']
})
export class BarGroupChartComponent implements OnInit {
  dataser1 = []
  dataser = {}

  // @ViewChild('barGroupChartContainer') barGroupChartContainer: ElementRef; old version
  @ViewChild('barGroupChartContainer', { static: true }) barGroupChartContainer: ElementRef;
  margin = { top: 20, right: 20, bottom: 30, left: 40 };
  @Input() width;
  @Input() height;
  @Input() data;
  x0: any;
  x1: any;
  y: any;
  xAxis: any;
  yAxis: any;
  color: any;
  svg: any;
  constructor() { }

  ngOnInit() { }

  ngOnChanges() {
    if (this.data) {
      this.data = this.transformData(this.data);
      this.createGroupBarGraph();
    }
  }

  transformData(data) {
    const arrayOfClusters = [];
    let innerFormat = {
      'clusterName': '',
      'values': []
    };
    const clusterName = Object.keys(data);

    for (const key in data) {
      if (data) {
        innerFormat.clusterName = key;
        let valuesNode = {};
        for (const index in data[key].Feature_Importance) {
          if (true) {
            valuesNode['frequencyValues'] = data[key].Feature_Importance[index];
            valuesNode['featureImportance'] = isNaN(data[key]['Frequency Values'][index]) ? 0.00 : data[key]['Frequency Values'][index];
            valuesNode['Count'] = data[key].Count[index];
            innerFormat.values.push(valuesNode);
            valuesNode = {};
          }
        }
        innerFormat.values.sort((a, b) =>
          (a.featureImportance > b.featureImportance) ? 1 : ((b.featureImportance > a.featureImportance) ? -1 : 0));
        arrayOfClusters.push(innerFormat);
        innerFormat = {
          'clusterName': '',
          'values': []
        };
      }
    }
    return arrayOfClusters;
  }


  createGroupBarGraph() {
    let additionalWidth;   
      if (this.data[0].values.length <= 3) {
      additionalWidth = this.data.length * this.data[0].values.length * 90;
    } else {
      additionalWidth = this.data.length * this.data[0].values.length * 40;
    }
    // const additionalWidth = this.data.length * this.data[0].values.length * 40;
    this.width = additionalWidth - this.margin.left - this.margin.right,
      this.height = 400 - this.margin.top - this.margin.bottom;
    this.createXYAxis();
    this.createSVGStructure();
    this.drawGraph();
  }

  createXYAxis() {
    this.x0 = d3.scaleBand()
      .rangeRound([0, this.width])
      .paddingInner(0.1);

    this.x1 = d3.scaleBand();

    this.y = d3.scaleLinear()
      .range([this.height, 0]);

    this.xAxis = d3.axisBottom()
      .scale(this.x0)
      .tickSize(0);

    this.yAxis = d3.axisLeft()
      .scale(this.y);
  }

  createSVGStructure() {
    this.color = d3.scaleOrdinal(d3.schemePaired);

    const element = this.barGroupChartContainer.nativeElement;
    element.innerText = '';

    this.svg = d3.select(element).append('svg')
      .attr('width', this.width + this.margin.left + this.margin.right)
      .attr('height', this.height + this.margin.top + this.margin.bottom)
      .append('g')
      .attr('transform', 'translate(' + this.margin.left + ',' + this.margin.top + ')');
  }

  drawGraph() {
    const clustername = this.data.map(function (d) { return d.clusterName; });
    const setofCluster = this.data[0].values.map(function (d) { return d.frequencyValues; });
    const _this = this;
    this.x0.domain(clustername);
    this.x1.domain(setofCluster).rangeRound([0, this.x0.bandwidth()]).padding(0.05);
    this.y.domain([
      d3.min(this.data, function (clusterName) { return d3.min(clusterName.values, function (d) { return d.featureImportance; }); }),
      d3.max(this.data, function (clusterName) { return d3.max(clusterName.values, function (d) { return d.featureImportance; }); })
    ]).nice();
    // constm= d3.min(this.data, function(clusterName) { return d3.min(clusterName.values, function(d) { return d.featureImportance; }); });
    // this.height = this.height -  Math.abs(_this.y(m) - _this.y(0));

    this.svg.append('g')
      .attr('class', 'x axis')
      .attr('transform', 'translate(0,' + this.height + ')')
      .call(this.xAxis)
      .attr('font-size', 14);

    this.svg.append('g')
      .attr('class', 'y axis')
      .style('opacity', '0')
      .call(this.yAxis)
      .append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 6)
      .attr('dy', '.71em')
      .style('text-anchor', 'end')
      .style('font-weight', 'bold')
      .text('Value');

    this.svg.select('.y').transition().duration(500).delay(1300).style('opacity', '1');

    const slice = this.svg.selectAll('.slice')
      .data(this.data)
      .enter().append('g')
      .attr('class', 'g')
      .attr('transform', function (d) { return 'translate(' + _this.x0(d.clusterName) + ',0)'; })
      .style('border-right', '1px solid black');

    const bar = slice.selectAll('rect')
      .data(function (d) { return d.values; })
      .enter();

    bar.append('rect')
      .attr('width', this.x1.bandwidth())
      .attr('x', function (d) { return _this.x1(d.frequencyValues); })
      .style('fill', function (d) { return _this.color(d.frequencyValues) })
      .attr('y', function (d) {
        if (d.featureImportance > 0) {
          return _this.y(d.featureImportance);
        } else {
          return _this.y(0);
        }
      })
      .attr('height', function (d) { return Math.abs(_this.y(d.featureImportance) - _this.y(0)); })
      .on('mouseover', function (d) {
        d3.select(this).style('fill', d3.rgb(_this.color(d.frequencyValues)).darker(2));
      })
      .on('mouseout', function (d) {
        d3.select(this).style('fill', _this.color(d.frequencyValues));
      });

    bar.append('text')
      .attr('class', 'label')
      // y position of the label is halfway down the bar
      .attr('y', function (item) {
        return _this.y(item.featureImportance) - 4;
      })
      // return ( _this.y(item.featureImportance) <= 0  ? 0 : (_this.y(item.featureImportance) - 14) );
      // x position is 3 pixels to the right of the bar
      .attr('x', function (item) {
        return _this.x1(item.frequencyValues) + _this.x1.bandwidth() / 2;
      })
      .attr('font-size', 12)
      .attr('font-family', 'sans-serif')
      .text(function (d) {
        return '';
        // return d.featureImportance.toFixed(3);
      });

    slice.append('line')
      .attr('class', 'line-class')
      .attr('x1', _this.x0.bandwidth() + 10)
      .attr('x2', _this.x0.bandwidth() + 10)
      .attr('y1', 0)
      .attr('y2', _this.height + _this.margin.top + _this.margin.bottom)
      .style('stroke-width', 1)
      .style('stroke-dasharray', ('3, 2'))
      .style('stroke', '#000');

    this.svg.select('.y').selectAll('.tick').append('line')
      .attr('class', 'line-class')
      .attr('x1', 0)
      .attr('x2', _this.width)
      .style('stroke-width', 1)
      .attr('opacity', '0.5')
      .style('stroke', 'lightblue');


    this.svg.selectAll('rect')
      .append('title')
      .text(function (d) {
        return d.frequencyValues + ': ' + d.featureImportance.toFixed(3); // + ' ' + d.Count;
      });

    // Legend
    // const legend = this.svg.selectAll('.legend')
    //   .data(_this.data[0].values.map(function (d) { return d.frequencyValues; }).reverse())
    //   .enter().append('g')
    //   .attr('class', 'legend')
    //   .attr('transform', function (d, i) { return 'translate(0,' + ( i * 20 + _this.height )  + ')'; })
    //   .style('opacity', '');

    // legend.append('rect')
    //   .attr('x', _this.width - 18)
    //   .attr('width', 18)
    //   .attr('height', 18)
    //   .style('fill', function (d) { return _this.color(d); });

    // legend.append('text')
    //   .attr('x', _this.width - 24)
    //   .attr('y', 9)
    //   .attr('dy', '.35em')
    //   .style('text-anchor', 'end')
    //   .text(function (d) { return d; });
    // slice.selectAll('rect')
    //   .attr('height', 0)
    //   .transition()
    //   .delay(function (d) { return Math.random() * 1000; })
    //   .duration(1000)
    //   .attr('height', function (d) { return _this.height - _this.y(d.featureImportance); });
  }

  checkColor(name) {
    return this.color(name);
  }
}
