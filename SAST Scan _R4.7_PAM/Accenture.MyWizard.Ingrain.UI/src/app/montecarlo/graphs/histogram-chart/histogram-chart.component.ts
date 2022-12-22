import { Component, ViewChild, ElementRef, Input, OnChanges } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-histogram-chart',
  templateUrl: './histogram-chart.component.html',
  styleUrls: ['./histogram-chart.component.scss']
})
export class HistogramChartComponent implements OnChanges {

  constructor() { }


  @ViewChild('containerHistogramChart', { static: true }) containerHdualChart: ElementRef;

  @Input() effortHistogram: Array<any>;
  @Input() certaintyValues: {};
  @Input() teamSizeHistogram: Array<any>;
  @Input() scheduleHistogram: Array<any>;
  @Input() effortValue: number;
  @Input() teamSizeValue: number;
  @Input() scheduleValue: number;

  efforData = [
    // 'Efforts': {}
  ];

  color = 'green';
  w = 416;// 360;
  h = 320; // 230;
  margin = { top: 20, right: 30, bottom: 60, left: 50 }; //  top: 20
  width = this.w - this.margin.left - this.margin.right;
  height = this.h - this.margin.top - this.margin.bottom;
  x; y; max; min; svg; element; xAxis; yAxis; data; yMax; yMin;
  values;
  teamSizeCertanityValues = 0; scheduleCertaintyVal = 0; effortCertaintyVal = 0;

  ngOnChanges() {
    this.element = this.containerHdualChart.nativeElement;
    this.deleteElementIfAlready(this.element);
    if (this.effortHistogram !== undefined) {
      this.createEffortHistogram(this.effortHistogram);
    } else if (this.scheduleHistogram !== undefined) {
      this.createScheduleChart(this.scheduleHistogram);
    } else if (this.teamSizeHistogram !== undefined) {
      this.createTeamSizeChart(this.teamSizeHistogram);
    }
  }


  getMaxValueforDualcolor(histoArray, certanityPer) {
    let xValue = 0; let xMaxValue = 0;
    const percentage = certanityPer / 100;
    const xMaxArrayGroup = Math.max.apply(Math, histoArray.map(function (d) { return d.XAxisValues; }));
    const xMinArrayGroup = Math.min.apply(Math, histoArray.map(function (d) { return d.XAxisValues; }));
    if (Math.sign(xMinArrayGroup) === -1) {
      xValue = (xMaxArrayGroup + Math.abs(xMinArrayGroup)) * percentage;
      xMaxValue = xValue - Math.abs(xMinArrayGroup);
    } else {
      // xValue = (xMaxArrayGroup - xMinArrayGroup) * percentage;
      // xMaxValue = xValue + xMinArrayGroup;
      xValue = (xMaxArrayGroup - Math.abs(xMinArrayGroup)) * percentage;
      xMaxValue = xValue + Math.abs(xMinArrayGroup);
    }
    return xMaxValue;
  }


  createEffortHistogram(data) {
    const height1 = this.height - this.margin.top - this.margin.bottom;
    const width1 = this.width - this.margin.left - this.margin.right;
    // const maxValue = Math.max.apply(Math, this.effortHistogram.map(function (d) { return d.YAxisValues; }));
    // console.log('max index', maxValue);

    this.effortCertaintyVal = this.certaintyValues['Effort (Hrs)'];

    const maxValueofRange = this.effortValue; // this.getMaxValueforDualcolor(this.effortHistogram, this.effortCertaintyVal);

    const svg = d3.select(this.element)
      .append('svg')
      .attr('width', width1 + this.margin.left + this.margin.right)
      .attr('height', height1 + this.margin.top + this.margin.bottom)
      .append('g')
      .attr('transform',
        'translate(' + this.margin.left + ',' + this.margin.top + ')');

    // X axis
    const x = d3.scaleBand()
      .range([0, width1])
      .domain(data.map(function (d) { return d.XAxisValues; }))
      .padding(0.2);
    svg.append('g')
      .attr('transform', 'translate(0,' + height1 + ')')
      .call(d3.axisBottom(x))
      .selectAll('text')
      .attr('transform', 'translate(-10,0)rotate(-45)')
      .style('text-anchor', 'end');

    // Add Y axis
    const y = d3.scaleLinear()
      .domain([d3.min(data, function (d) {
        return d.YAxisValues;
      }), d3.max(data, function (d) {
        return d.YAxisValues;
      })])
      // .domain([0, data.map(function (d) { return d.YAxisValues; })])
      .range([height1, 0]);
    svg.append('g')
      .call(d3.axisLeft(y));

    // Bars
    svg.selectAll('.bar')
      .data(data)
      .enter()
      .append('rect')
      .attr('x', function (d) { return x(d.XAxisValues); })
      .attr('y', function (d) { return y(d.YAxisValues); })
      .attr('width', x.bandwidth())
      .attr('height', function (d) { return height1 - y(d.YAxisValues); })
      // .attr('fill', 'green');
      .attr('fill', function (d) {
        if (d.XAxisValues <= maxValueofRange) {
          return 'green';
        } else { return '#990000'; }
      });


    // Add axis labels
    svg.append('text')
      .attr('class', 'x label')
      .attr('transform', 'translate(' + (width1 / 2) + ' ,' + (height1 + this.margin.bottom - this.margin.top) + ')')
      // .attr('dy', '1em')
      .attr('text-anchor', 'middle')
      .attr('dy', '0.6em')
      .style('font-size', '0.8rem')
      .text('Certainty ' + this.effortCertaintyVal + '%');

    svg.append('text')
      .attr('class', 'y label')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - this.margin.left)
      .attr('x', 0 - (height1 / 2))
      .attr('dy', '0.8em')
      .attr('text-anchor', 'middle')
      .style('font-size', '0.9rem')
      .text('Frequency');
  }

  createScheduleChart(data) {
    const height1 = this.height - this.margin.top - this.margin.bottom;
    const width1 = this.width - this.margin.left - this.margin.right;

    // const maxValue = Math.max.apply(Math, this.scheduleHistogram.map(function (d) { return d.YAxisValues; }));
    // console.log('max index', maxValue);

    this.scheduleCertaintyVal = this.certaintyValues['Schedule (Days)'];
    const maxValueofRange = this.scheduleValue; // this.getMaxValueforDualcolor(this.scheduleHistogram, this.scheduleCertaintyVal);

    const svg = d3.select(this.element)
      .append('svg')
      .attr('width', width1 + this.margin.left + this.margin.right)
      .attr('height', height1 + this.margin.top + this.margin.bottom)
      .append('g')
      .attr('transform',
        'translate(' + this.margin.left + ',' + this.margin.top + ')');

    // X axis
    const x = d3.scaleBand()
      .range([0, width1])
      .domain(data.map(function (d) { return d.XAxisValues; }))
      .padding(0.2);
    svg.append('g')
      .attr('transform', 'translate(0,' + height1 + ')')
      .call(d3.axisBottom(x))
      .selectAll('text')
      .attr('transform', 'translate(-10,0)rotate(-45)')
      .style('text-anchor', 'end');

    // Add Y axis
    const y = d3.scaleLinear()
      .domain([d3.min(data, function (d) {
        return d.YAxisValues;
      }), d3.max(data, function (d) {
        return d.YAxisValues;
      })])
      // .domain([0, data.map(function (d) { return d.YAxisValues; })])
      .range([height1, 0]);
    svg.append('g')
      .call(d3.axisLeft(y));

    // Bars
    svg.selectAll('.bar')
      .data(data)
      .enter()
      .append('rect')
      .attr('x', function (d) { return x(d.XAxisValues); })
      .attr('y', function (d) { return y(d.YAxisValues); })
      .attr('width', x.bandwidth())
      .attr('height', function (d) { return height1 - y(d.YAxisValues); })
      // .attr('fill', 'green');
      .attr('fill', function (d) {
        if (d.XAxisValues <= maxValueofRange) {
          return 'green';
        } else { return '#990000'; }
      });


    // Add axis labels
    svg.append('text')
      .attr('class', 'x label')
      .attr('transform', 'translate(' + (width1 / 2) + ' ,' + (height1 + this.margin.bottom - this.margin.top) + ')')
      // .attr('dy', '1em')
      .attr('text-anchor', 'middle')
      .attr('dy', '0.6em')
      .style('font-size', '0.8rem')
      .text('Certainty ' + this.scheduleCertaintyVal + '%');

    svg.append('text')
      .attr('class', 'y label')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - this.margin.left)
      .attr('x', 0 - (height1 / 2))
      .attr('dy', '0.8em')
      .attr('text-anchor', 'middle')
      .style('font-size', '0.9rem')
      .text('Frequency');
  }

  createTeamSizeChart(data) {
    const height1 = this.height - this.margin.top - this.margin.bottom;
    const width1 = this.width - this.margin.left - this.margin.right;

    this.teamSizeCertanityValues = this.certaintyValues['Team Size'];
    const maxValueofRange = this.teamSizeValue; // this.getMaxValueforDualcolor(this.teamSizeHistogram, this.teamSizeCertanityValues);

    const svg = d3.select(this.element)
      .append('svg')
      .attr('width', width1 + this.margin.left + this.margin.right)
      .attr('height', height1 + this.margin.top + this.margin.bottom)
      .append('g')
      .attr('transform',
        'translate(' + this.margin.left + ',' + this.margin.top + ')');

    // X axis
    const x = d3.scaleBand()
      .range([0, width1])
      .domain(data.map(function (d) { return d.XAxisValues; }))
      .padding(0.2);
    svg.append('g')
      .attr('transform', 'translate(0,' + height1 + ')')
      .call(d3.axisBottom(x))
      .selectAll('text')
      .attr('transform', 'translate(-10,0)rotate(-45)')
      .style('text-anchor', 'end');

    // Add Y axis
    const y = d3.scaleLinear()
      .domain([d3.min(data, function (d) {
        return d.YAxisValues;
      }), d3.max(data, function (d) {
        return d.YAxisValues;
      })])
      // .domain([0, data.map(function (d) { return d.YAxisValues; })])
      .range([height1, 0]);
    svg.append('g')
      .call(d3.axisLeft(y));

    // Bars
    svg.selectAll('.bar')
      .data(data)
      .enter()
      .append('rect')
      .attr('x', function (d) { return x(d.XAxisValues); })
      .attr('y', function (d) { return y(d.YAxisValues); })
      .attr('width', x.bandwidth())
      .attr('height', function (d) { return height1 - y(d.YAxisValues); })
      // .attr('fill', 'green');
      .attr('fill', function (d) {
        if (d.XAxisValues <= maxValueofRange) {
          return 'green';
        } else { return '#990000'; }
      });


    // Add axis labels
    svg.append('text')
      .attr('class', 'x label')
      .attr('transform', 'translate(' + (width1 / 2) + ' ,' + (height1 + this.margin.bottom - this.margin.top) + ')')
      // .attr('dy', '1em')
      .attr('text-anchor', 'middle')
      .attr('dy', '0.6em')
      .style('font-size', '0.8rem')
      .text('Certainty ' + this.teamSizeCertanityValues + '%');

    svg.append('text')
      .attr('class', 'y label')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - this.margin.left)
      .attr('x', 0 - (height1 / 2))
      .attr('dy', '0.8em')
      .attr('text-anchor', 'middle')
      .style('font-size', '0.9rem')
      .text('Frequency');
  }

  deleteElementIfAlready(element: any) {
    if (element.hasChildNodes()) {
      let eleLength = element.childNodes.length;
      do {
        const childNodes = Array.from(element.childNodes);

        Array.prototype.forEach.call(childNodes, function (childNode) {
          childNode.remove();
        });

        eleLength = element.childNodes.length;
      } while (eleLength > 0);
    }
  }

}
