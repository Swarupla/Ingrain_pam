import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DeployCascadeModelsComponent } from './deploy-cascade-models.component';

describe('DeployCascadeModelsComponent', () => {
  let component: DeployCascadeModelsComponent;
  let fixture: ComponentFixture<DeployCascadeModelsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DeployCascadeModelsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DeployCascadeModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
