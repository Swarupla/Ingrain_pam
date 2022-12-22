import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MapCascadeModelsComponent } from './map-cascade-models.component';

describe('MapCascadeModelsComponent', () => {
  let component: MapCascadeModelsComponent;
  let fixture: ComponentFixture<MapCascadeModelsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MapCascadeModelsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MapCascadeModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
