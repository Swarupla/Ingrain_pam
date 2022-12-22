import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CascadeVisualizationGraphComponent } from './cascade-visualization-graph.component';

describe('CascadeVisualizationGraphComponent', () => {
  let component: CascadeVisualizationGraphComponent;
  let fixture: ComponentFixture<CascadeVisualizationGraphComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CascadeVisualizationGraphComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CascadeVisualizationGraphComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
